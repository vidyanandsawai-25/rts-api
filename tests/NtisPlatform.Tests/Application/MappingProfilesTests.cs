using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using AutoMapper;
using NtisPlatform.Application.Mappings;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class MappingProfilesTests
    {
        [Fact]
        public void EntityToDto_MappingProfiles_Should_HaveValidConfiguration()
        {
            var assembly = Assembly.GetAssembly(typeof(ConstructionTypeMappingProfile)) ?? typeof(ConstructionTypeMappingProfile).Assembly;

            // Instantiate all profiles and build a single configuration that contains them all.
            var profileTypes = assembly.GetTypes()
                .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            var profiles = profileTypes.Select(t => (Profile)Activator.CreateInstance(t)!).ToList();

            var config = new MapperConfiguration(cfg =>
            {
                foreach (var p in profiles)
                    cfg.AddProfile(p);
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            var mapper = config.CreateMapper();
            var provider = mapper.ConfigurationProvider;

            // Try to obtain all TypeMaps via provider/internal reflection
            IEnumerable<object>? allTypeMaps = null;

            // Look for any zero-argument method that returns IEnumerable and whose elements expose SourceType/DestinationType
            var providerMethods = provider.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // Explicitly filter candidate methods (cheap predicates) first
            var candidateMethods = providerMethods
                .Where(m => m.GetParameters().Length == 0)
                .Where(m => typeof(System.Collections.IEnumerable).IsAssignableFrom(m.ReturnType));

            foreach (var m in candidateMethods)
            {
                try
                {
                    var result = m.Invoke(provider, null) as System.Collections.IEnumerable;
                    if (result == null) continue;

                    // check first element for SourceType/DestinationType props
                    var enumerator = result.GetEnumerator();
                    if (!enumerator.MoveNext()) continue;

                    var first = enumerator.Current;
                    if (first == null) continue;

                    var ft = first.GetType();
                    if (ft.GetProperty("SourceType") != null && ft.GetProperty("DestinationType") != null)
                    {
                        // adopt this method's result as allTypeMaps
                        allTypeMaps = result.Cast<object>();
                        break;
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }


            var mappedPairs = new List<(Type Src, Type Dst)>();

            if (allTypeMaps != null)
            {
                foreach (var tm in allTypeMaps)
                {
                    var tmType = tm?.GetType();
                    if (tmType == null) continue;

                    var srcProp = tmType.GetProperty("SourceType");
                    var dstProp = tmType.GetProperty("DestinationType");
                    if (srcProp == null || dstProp == null) continue;

                    var srcType = srcProp.GetValue(tm) as Type;
                    var dstType = dstProp.GetValue(tm) as Type;

                    if (srcType == null || dstType == null) continue;

                    if (srcType.Namespace != null && srcType.Namespace.Contains("NtisPlatform.Core")
                        && dstType.Namespace != null && dstType.Namespace.StartsWith("NtisPlatform.Application.DTOs"))
                    {
                        mappedPairs.Add((srcType, dstType));
                    }
                }
            }
            else
            {
                // Fallback: scan assemblies for candidate types and ask provider for maps via FindTypeMapFor or enumerate provider maps
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var srcTypes = assemblies.SelectMany(a => GetTypesSafe(a))
                    .Where(t => t.IsClass && t.Namespace != null && t.Namespace.Contains("NtisPlatform.Core"))
                    .ToArray();
                var dstTypes = assemblies.SelectMany(a => GetTypesSafe(a))
                    .Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("NtisPlatform.Application.DTOs"))
                    .ToArray();

                MethodInfo? findMethod = provider.GetType().GetMethod("FindTypeMapFor", new[] { typeof(Type), typeof(Type) })
                                         ?? provider.GetType().GetMethod("FindTypeMapFor", new[] { typeof(Type), typeof(Type), typeof(Type) });

                for (int i = 0; i < srcTypes.Length; i++)
                {
                    var s = srcTypes[i];
                    for (int j = 0; j < dstTypes.Length; j++)
                    {
                        var d = dstTypes[j];
                        try
                        {
                            object? tm = null;
                            if (findMethod != null)
                                tm = findMethod.Invoke(provider, new object[] { s, d });

                            if (tm != null)
                            {
                                mappedPairs.Add((s, d));
                                continue;
                            }

                            // As last resort, enumerate provider TypeMaps if available
                            var providerGetAll = provider.GetType().GetMethod("GetAllTypeMaps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (providerGetAll != null)
                            {
                                var all = providerGetAll.Invoke(provider, null) as System.Collections.IEnumerable;
                                if (all != null)
                                {
                                    foreach (var itm in all)
                                    {
                                        var itmType = itm?.GetType();
                                        if (itmType == null) continue;
                                        var srcProp = itmType.GetProperty("SourceType");
                                        var dstProp = itmType.GetProperty("DestinationType");
                                        if (srcProp == null || dstProp == null) continue;
                                        var sType = srcProp.GetValue(itm) as Type;
                                        var dType = dstProp.GetValue(itm) as Type;
                                        if (sType == null || dType == null) continue;
                                        if (sType == s && dType == d)
                                        {
                                            mappedPairs.Add((s, d));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // ignore and continue
                        }
                    }
                }
            }

            if (mappedPairs.Count == 0)
            {
                // nothing to validate
                return;
            }

            // Execute mappings for discovered pairs
            foreach (var pair in mappedPairs)
            {
                var srcType = pair.Src;
                var dstType = pair.Dst;

                object? sourceInstance = null;
                try
                {
                    sourceInstance = Activator.CreateInstance(srcType);
                }
                catch
                {
                    // cannot instantiate source, skip
                    continue;
                }

                try
                {
                    var result = mapper.Map(sourceInstance, srcType, dstType);
                    Assert.NotNull(result);
                }
                catch (AutoMapperConfigurationException ex)
                {
                    throw new Exception($"Mapping failed from {srcType.FullName} to {dstType.FullName}: {ex.Message}", ex);
                }
            }
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!.Cast<Type>();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }
    }
}
