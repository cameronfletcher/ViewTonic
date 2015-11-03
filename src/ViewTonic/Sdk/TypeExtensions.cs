﻿// <copyright file="OrderedItem.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains extension methods for <see cref="System.Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Enumerates the type hierarchy until the specified type is reached.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="type">The type to enumerate until.</param>
        /// <returns>The type hierarchy until the specified type.</returns>
        public static IEnumerable<Type> GetTypeHierarchyUntil(this Type sourceType, Type type)
        {
            Guard.Against.Null(() => sourceType);
            Guard.Against.Null(() => type);

            do
            {
                yield return sourceType;
            }
            while ((sourceType = sourceType.BaseType) != type && sourceType != null);
        }


        /// <summary>
        /// Determines whether the type is a subclass of the specified raw generic type.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="genericType">The raw generic type.</param>
        /// <returns>Returns <c>true</c> if the type is a subclass of the specified raw generic type.</returns>
        public static bool IsSubclassOfRawGeneric(this Type sourceType, Type genericType)
        {
            while (sourceType != null && sourceType != typeof(object))
            {
                var currentType = sourceType.IsGenericType ? sourceType.GetGenericTypeDefinition() : sourceType;
                if (currentType == genericType)
                {
                    return true;
                }

                sourceType = sourceType.BaseType;
            }

            return false;
        }
    }
}