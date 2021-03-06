﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Umbraco.Core;

namespace Our.Umbraco.Ditto
{
    /// <summary>
    /// Extensions methods for <see cref="T:System.Type"/> for inferring type properties.
    /// Most of this code was adapted from the Entity Framework
    /// </summary>
    internal static class TypeInferenceExtensions
    {
        /// <summary>
        /// The cache for storing constructor parameter information.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ParameterInfo[]> ConstructorCache
            = new ConcurrentDictionary<Type, ParameterInfo[]>();

        /// <summary>
        /// Returns a collection of parameter info for the given type. 
        /// Results are cached on subsequent usage.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="T:ParameterInfo[]"/>.</returns>
        public static ParameterInfo[] GetConstructorParameters(this Type type)
        {
            // Get the default constructor, parameters and create an instance of the type.
            // Try and return from the cache first. TryGetValue is faster than GetOrAdd.
            ParameterInfo[] constructorParams;
            ConstructorCache.TryGetValue(type, out constructorParams);
            if (constructorParams == null)
            {
                var constructor = type.GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();
                if (constructor != null)
                {
                    constructorParams = constructor.GetParameters();
                    ConstructorCache.TryAdd(type, constructorParams);
                }
            }

            return constructorParams;
        }

        /// <summary>
        /// Determines whether the specified type is an enumerable of the given argument type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="typeArgument">The generic type argument.</param>
        /// <returns>
        /// True if the type is an enumerable of the given argument type; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnumerableOfType(this Type type, Type typeArgument)
        {
            Type t = type.TryGetElementType(typeof(IEnumerable<>));
            return t != null && typeArgument.IsAssignableFrom(t);
        }

        /// <summary>
        /// Determines whether the specified type is a collection type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is a collection type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionType(this Type type)
        {
            return type.TryGetElementType(typeof(ICollection<>)) != null;
        }

        /// <summary>
        /// Determines whether the specified type is an enumerable type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is an enumerable type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnumerableType(this Type type)
        {
            return type.TryGetElementType(typeof(IEnumerable<>)) != null;
        }

        /// <summary>
        /// Determines whether the specified type is an enumerable type containing a 
        /// key value pair as the generic type parameter.
        /// <remarks>
        /// <see cref="M:Enumerable.FirstOrDefault"/> will throw an error when passed an
        /// <see cref="T:IEnumerable{KeyValuePair{,}}"/> this includes <see cref="T:Dictionary{,}"/>.
        /// </remarks>
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// True if the type is an enumerable type with the generic parameter of a key/value 
        /// pair; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnumerableOfKeyValueType(this Type type)
        {
            return type.TryGetElementType(typeof(IDictionary<,>)) != null ||
                (type.IsEnumerableType() && type.IsGenericType && type.GenericTypeArguments.Any()
                 && type.GenericTypeArguments[0].IsGenericType
                 && type.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
        }

        /// <summary>
        /// Determines whether the specified type is an enumerable type that is safe to cast
        /// following processing via a type converter.
        /// <remarks>
        /// This should exclude <see cref="T:string"/>, <see cref="T:Dictionary{,}"/>
        /// </remarks>
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is a cast-safe, enumerable type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCastableEnumerableType(this Type type)
        {
            // String, though enumerable have no generic arguments.
            // Types with more than one generic argument cannot be cast. 
            // Dictionary, though enumerable, requires linq to convert and shouldn't be attempted anyway.
            return type.IsEnumerableType() && type.GenericTypeArguments.Any()
                    && type.GenericTypeArguments.Length == 1
                    && type.TryGetElementType(typeof(IDictionary<,>)) == null;
        }

        /// <summary>
        /// Determine if the given type implements the given generic interface or derives from the given generic type,
        /// and if so return the element type of the collection. If the type implements the generic interface several times
        /// <c>null</c> will be returned.
        /// </summary>
        /// <param name="type">The type to examine. </param>
        /// <param name="interfaceOrBaseType"> The generic type to be queried for. </param>
        /// <returns> 
        /// <c>null</c> if <paramref name="interfaceOrBaseType"/> isn't implemented or implemented multiple times,
        /// otherwise the generic argument.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type TryGetElementType(this Type type, Type interfaceOrBaseType)
        {
            if (!type.IsGenericTypeDefinition)
            {
                Type[] types = GetGenericTypeImplementations(type, interfaceOrBaseType).ToArray();
                return types.Length == 1 ? types[0].GetGenericArguments().FirstOrDefault() : null;
            }

            return null;
        }

        /// <summary>
        /// Determine if the given type implements the given generic interface or derives from the given generic type,
        /// and if so return the concrete types implemented.
        /// </summary>
        /// <param name="type"> The type to examine. </param>
        /// <param name="interfaceOrBaseType"> The generic type to be queried for. </param>
        /// <returns> 
        /// The generic types constructed from <paramref name="interfaceOrBaseType"/> and implemented by <paramref name="type"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Type> GetGenericTypeImplementations(this Type type, Type interfaceOrBaseType)
        {
            if (!type.IsGenericTypeDefinition)
            {
                return (interfaceOrBaseType.IsInterface ? type.GetInterfaces() : type.GetBaseTypes())
                        .Union(new[] { type })
                        .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceOrBaseType);
            }

            return Enumerable.Empty<Type>();
        }

        /// <summary>
        /// Gets the base types that the given type inherits from
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get the base types from.</param>
        /// <returns>A collection of base types that the given type inherits from.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            type = type.BaseType;

            while (type != null)
            {
                yield return type;

                type = type.BaseType;
            }
        }

        /// <summary>
        /// Gets the type of the enumerable object
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns>The type of the enumerable.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetEnumerableType(this Type type)
        {
            // if it's not an enumerable why do you call this method all ?
            if (!type.IsEnumerable())
            {
                return null;
            }

            var interfaces = type.GetInterfaces().ToList();
            if (type.IsInterface && interfaces.All(i => i != type))
            {
                interfaces.Add(type);
            }

            return interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(i => i.GetGenericArguments()[0]).FirstOrDefault();
        }
    }
}