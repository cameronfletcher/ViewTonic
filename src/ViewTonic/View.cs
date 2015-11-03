// <copyright file="View.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using ViewTonic.Persistence;
    using ViewTonic.Sdk;

    public class View
    {
        private static readonly string DefaultMethodName = GetDefaultMethodName();

        private readonly Dictionary<Type, List<Action<object, object>>> handlers;

        private List<ISnapshotRepository> repositories;

        public View()
            : this(DefaultMethodName)
        {
        }

        public View(string methodName)
            : this(methodName, BindingFlags.Instance | BindingFlags.Public)
        {
        }

        public View(string methodName, BindingFlags bindingFlags)
        {
            Guard.Against.NullOrEmpty(() => methodName);

            if (!CodeGenerator.IsValidLanguageIndependentIdentifier(methodName))
            {
                throw new ArgumentException("The specified target method name is not a valid language independent identifier.", "targetMethodName");
            }

            this.handlers = GetHandlers(this.GetType(), methodName, bindingFlags);
        }

        internal void Apply(object @event)
        {
            this.Dispatch(this, @event);
        }

        public void Snapshot()
        {
            if (this.repositories == null)
            {
                this.repositories = this.GetType().GetTypeHierarchyUntil(typeof(View))
                    .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    .Where(field => field.FieldType.IsSubclassOfRawGeneric(typeof(IRepository<,>)))
                    .Select(field => field.GetValue(this))
                    .Select(repository => repository as ISnapshotRepository)
                    .ToList();

                if (this.repositories.Any(repository => repository == null))
                {
                    throw new InvalidOperationException("One or more of the view repositories cannot be flushed.");
                }
            }

            this.repositories.ForEach(repository => repository.TakeSnapshot());
        }

        public void Flush()
        {
            if (this.repositories == null)
            {
                this.repositories = this.GetType().GetTypeHierarchyUntil(typeof(View))
                    .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    .Where(field => field.FieldType.IsSubclassOfRawGeneric(typeof(IRepository<,>)))
                    .Select(field => field.GetValue(this))
                    .Select(repository => repository as ISnapshotRepository)
                    .ToList();

                if (this.repositories.Any(repository => repository == null))
                {
                    throw new InvalidOperationException("One or more of the view repositories cannot be flushed.");
                }
            }

            this.repositories.ForEach(repository => repository.FlushSnapshot());
        }

        private void Dispatch(object target, object @event)
        {
            Guard.Against.Null(() => target);
            Guard.Against.Null(() => @event);

            var handlerList = default(List<Action<object, object>>);
            if (this.handlers.TryGetValue(@event.GetType(), out handlerList))
            {
                foreach (var handler in handlerList)
                {
                    handler.Invoke(target, @event);
                }
            }
        }

        private static Dictionary<Type, List<Action<object, object>>> GetHandlers(Type type, string methodName, BindingFlags bindingFlags)
        {
            var handlerMethods = type.GetTypeHierarchyUntil(typeof(View))
                .SelectMany(t => t.GetMethods(bindingFlags))
                .Where(method => method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                .Where(method => method.GetParameters().Count() == 1)
                .Where(method => method.DeclaringType != typeof(object))
                .Select(methodInfo =>
                    new
                    {
                        Info = methodInfo,
                        ParameterType = methodInfo.GetParameters().First().ParameterType,
                    })
                .ToArray();

            var invalidHandlerMethodTypes = handlerMethods
                .Where(method => !method.ParameterType.IsClass)
                .ToArray();

            var handlers = new Dictionary<Type, List<Action<object, object>>>();

            foreach (var handlerMethod in handlerMethods.Except(invalidHandlerMethodTypes))
            {
                var handler = CreateHandlerDelegate(type, handlerMethod.Info);
                var handlerList = default(List<Action<object, object>>);
                if (!handlers.TryGetValue(handlerMethod.ParameterType, out handlerList))
                {
                    handlerList = new List<Action<object, object>>();
                    handlers.Add(handlerMethod.ParameterType, handlerList);
                }

                handlerList.Add(handler);
            }

            return handlers;
        }

        // LINK (Cameron): http://www.sapiensworks.com/blog/post/2012/04/19/Invoking-A-Private-Method-On-A-Subclass.aspx
        private static Action<object, object> CreateHandlerDelegate(Type declaringType, MethodInfo methodInfo)
        {
            var dynamicMethod = new DynamicMethod(
                string.Empty,
                typeof(void),
                new[] { typeof(object), typeof(object) },
                declaringType.Module,
                true);

            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);          // load this
            il.Emit(OpCodes.Ldarg_1);          // load event
            il.Emit(OpCodes.Call, methodInfo); // call apply method
            il.Emit(OpCodes.Ret);              // return

            return dynamicMethod.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }

        // LINK (Cameron): http://blog.functionalfun.net/2009/10/getting-methodinfo-of-generic-method.html
        private static string GetDefaultMethodName()
        {
            Expression<Action<View>> expression = view => view.Consume(default(object));
            var lambda = (LambdaExpression)expression;
            var methodCall = (MethodCallExpression)lambda.Body;
            return methodCall.Method.Name;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "By design.")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "event", Justification = "Also, by design.")]
        private void Consume(object @event)
        {
        }

    }
}
