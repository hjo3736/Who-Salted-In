using System.Linq;
using Game.Core;
using Game.Replay;
using NUnit.Framework;

namespace Game.Tests
{
    public class ArchitectureTests
    {
        static string[] EngineReferencesOf(System.Type typeInAssembly) =>
            typeInAssembly.Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

        [Test]
        public void GameCore_HasNoEngineReferences()
        {
            var leaks = EngineReferencesOf(typeof(KitchenState));
            Assert.IsEmpty(leaks, "Game.Core leaked engine references: " + string.Join(", ", leaks));
        }

        [Test]
        public void GameReplay_HasNoEngineReferences()
        {
            var leaks = EngineReferencesOf(typeof(CameraSample));
            Assert.IsEmpty(leaks, "Game.Replay leaked engine references: " + string.Join(", ", leaks));
        }
    }
}
