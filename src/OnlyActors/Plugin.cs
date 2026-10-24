using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using System.Collections.Generic;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Querying;

namespace OnlyActors
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool OnlyActors { get; set; } = true;
        public bool HideExternalUrls { get; set; } = true;
        public bool HideStudios { get; set; } = true;
        public bool HideTags { get; set; } = true;
    }

    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "MadbyEnhance3",
                    DisplayName = "Madby增强",
                    EnableInMainMenu = true,
                    MenuIcon = "person",
                    EmbeddedResourcePath = "OnlyActors.ConfigurationPage.html"
                },
                new PluginPageInfo
                {
                    Name = "MadbyEnhance3js",
                    DisplayName = "Madby增强",
                    EnableInMainMenu = false,
                    EmbeddedResourcePath = "OnlyActors.ConfigurationPage.js"
                }
            };
        }

        public static Plugin Instance { get; private set; }
        public static string PatchStatus = "not-run";
        public static string DataDir = "/config/plugins/configurations";
        private static bool _patched;

        public override Guid Id => new Guid("7c2f4d31-9b6e-4a52-8f0d-1e5c3a9b7d24");
        public override string Name => "Madby增强";
        public override string Description => "Madby 增强：人物列表只显示演员（含剧集演员），隐藏导演/编剧等职员（依赖 Madby 提供的 Harmony）";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            try { DataDir = applicationPaths.PluginConfigurationsPath; } catch { }

            // 复用进程里已加载的 Harmony（Madby 已加载 0Harmony），避免自带版本与它冲突
            AppDomain.CurrentDomain.AssemblyLoad += (s, e) =>
            {
                if (e.LoadedAssembly.GetName().Name == "0Harmony") TryPatch();
            };
            TryPatch();
            Log("ctor " + PatchStatus);
        }

        private static MethodBase FindTarget()
        {
            var t = Type.GetType("Emby.Server.Implementations.Dto.DtoService, Emby.Server.Implementations", false)
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType("Emby.Server.Implementations.Dto.DtoService", false))
                        .FirstOrDefault(x => x != null);
            if (t == null) return null;
            return t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "AttachPeople");
        }

        private static void TryPatch()
        {
            if (_patched) return;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly harmonyAsm = null;
                Type harmonyTypeProbe = null;
                foreach (var a in assemblies)
                {
                    try
                    {
                        var tt = a.GetType("HarmonyLib.Harmony", false);
                        if (tt != null) { harmonyAsm = a; harmonyTypeProbe = tt; break; }
                    }
                    catch { }
                }
                if (harmonyAsm == null)
                {
                    PatchStatus = "waiting-harmony asm=[" + string.Join(",", assemblies.Select(x => x.GetName().Name)) + "]";
                    return;
                }

                var target = FindTarget();
                if (target == null) { PatchStatus = "target-not-found"; return; }

                var harmonyType = harmonyAsm.GetType("HarmonyLib.Harmony");
                var methodType = harmonyAsm.GetType("HarmonyLib.HarmonyMethod");
                if (harmonyType == null || methodType == null) { PatchStatus = "harmony-types-missing"; return; }

                var harmony = Activator.CreateInstance(harmonyType, "hermes.onlyactors");
                var patch = harmonyType.GetMethod("Patch", new[] { typeof(MethodBase), methodType, methodType, methodType, methodType });

                var postfix = Activator.CreateInstance(methodType, typeof(PeopleFilter).GetMethod(nameof(PeopleFilter.Postfix)));
                patch.Invoke(harmony, new object[] { target, null, postfix, null, null });

                var fieldsTarget = AccessToolsMethod(target.DeclaringType, "AttachBasicFields");
                if (fieldsTarget != null)
                {
                    var fp = Activator.CreateInstance(methodType, typeof(FieldsFilter).GetMethod(nameof(FieldsFilter.Postfix)));
                    patch.Invoke(harmony, new object[] { fieldsTarget, null, fp, null, null });
                }

                _patched = true;
                PatchStatus = "patched:" + target.DeclaringType.FullName + "." + target.Name
                              + " params=" + string.Join(",", target.GetParameters().Select(x => x.ParameterType.Name))
                              + " harmony=" + harmonyAsm.GetName().Version;
            }
            catch (Exception ex)
            {
                var e = ex.InnerException ?? ex;
                PatchStatus = "patch-error:" + e.GetType().Name + ":" + e.Message;
            }
        }

        public static MethodBase AccessToolsMethod(Type t, string name)
        {
            return t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == name);
        }

        public static void Log(string msg)
        {
            try { File.AppendAllText("/tmp/onlyactors-boot.log", DateTime.Now.ToString("O") + " " + msg + "\n"); } catch { }
        }
    }

    public static class FieldsFilter
    {
        public static void Postfix(BaseItemDto __0)
        {
            try
            {
                var cfg = Plugin.Instance?.Configuration;
                var dto = __0;
                if (cfg == null || dto == null) return;
                if (cfg.HideExternalUrls && dto.ExternalUrls != null && dto.ExternalUrls.Length > 0)
                    dto.ExternalUrls = new ExternalUrl[0];
                if (cfg.HideStudios && dto.Studios != null && dto.Studios.Length > 0)
                    dto.Studios = new NameLongIdPair[0];
                if (cfg.HideTags && dto.Tags != null && dto.Tags.Length > 0)
                    dto.Tags = new string[0];
                if (cfg.HideTags && dto.TagItems != null && dto.TagItems.Length > 0)
                    dto.TagItems = new NameLongIdPair[0];
            }
            catch { }
        }
    }

    public static class PeopleFilter
    {
        public static int Calls;

        public static void Postfix(BaseItemDto __0)
        {
            var dto = __0;
            if (dto?.People == null) return;
            int n = Interlocked.Increment(ref Calls);
            if (n <= 5)
                Plugin.Log("call " + n + " people=" + dto.People.Length + " types=" +
                          string.Join("|", dto.People.Select(x => x.Type.ToString()).Distinct()) +
                          " cfg=" + (Plugin.Instance?.Configuration?.OnlyActors));
            if (Plugin.Instance?.Configuration?.OnlyActors != true) return;
            var kept = dto.People.Where(p => p != null && (p.Type == PersonType.Actor || p.Type == PersonType.GuestStar)).ToArray();
            if (kept.Length != dto.People.Length) dto.People = kept;
        }
    }
}
