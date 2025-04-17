using System.Collections;
using System.Text.RegularExpressions;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone;
using MelonLoader;
using UnityEngine;
using HarmonyLib;

[assembly: MelonInfo(typeof(SCreenSignCommandsIL2CPP.Core), "SCreenSignCommandsIL2CPP", "1.0.0", "animandan", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SCreenSignCommandsIL2CPP
{
    public class Core : MelonMod
    {
        public List<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem> labelledSurfaceItems;
        public SignCommands signCommands;
        public float waittime = 1f;
        public float innerwaittime = 0.1f;

        public override void OnInitializeMelon()
        {
            this.labelledSurfaceItems = new List<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem>();
            this.signCommands = new SignCommands();
            LoggerInstance.Msg("Initialized.");
        }

        public void register_command(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, string> command_method)
        {
            this.signCommands.register_command(name, description, command_method);
        }

        public void register_command(SignCommand signCommand)
        {
            this.signCommands.register_command((SignCommand)signCommand);
        }


        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                Commander commander = new Commander();
                commander.RunCommands();
            }
        }

        public class Commander : MonoBehaviour
        {

            public void RunCommands()
            {
                object routine = MelonCoroutines.Start(RunCommandsRoutine());
            }

            public IEnumerator RunCommandsRoutine()
            {
                
                while (PlayerSingleton<AppsCanvas>.Instance == null)
                    yield return new WaitForSeconds(Melon<Core>.Instance.waittime);
                while (true)
                {
                    foreach (Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem item in Melon<Core>.Instance.labelledSurfaceItems)
                    {
                        yield return new WaitForSeconds(Melon<Core>.Instance.innerwaittime);
                        Melon<Core>.Logger.Msg($"IN! {item.Message}");
                        try
                        {
                            Melon<Core>.Instance.signCommands.run_commands(item, item.Message);
                        }
                        catch (System.Exception ex)
                        {
                            Melon<Core>.Logger.Msg($"error: {ex}");
                        }

                    }
                    yield return new WaitForSeconds(Melon<Core>.Instance.waittime);
                }
            }
        }


    }

    public class SignCommand
    {
        public string name;
        public string description;
        public Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, string> _command_function;


        public string run(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem instance)
        {
            return this._command_function(instance);
        }

        public SignCommand(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, string> command_function)
        {
            this.name = name;
            this.description = description;
            this._command_function = command_function;
        }

        public static Dictionary<string, string> getArgs(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem instance)
        {
            string message = instance.Message;
            string command_name = message.Split(' ').FirstOrDefault();
            string remaining_text = removeFirstInstance(message, $"{command_name}");
            string pattern = @"(?<=\s)(\w+=(?:""(?:(?<=\\)""|[^""])+""|[\S]+))";
            RegexOptions options = RegexOptions.Multiline;
            Dictionary<string, string> args = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(message, pattern, options))
            {
                Melon<Core>.Logger.Msg("'{0}' found at index {1}.", m.Value, m.Index);
                remaining_text = removeFirstInstance(remaining_text, $"{m.Value}");
                string[] kv = m.Value.Split("=", 2);
                args.Add(kv[0], kv[1]);
                Melon<Core>.Logger.Msg("Key: {0}' Value: {1}.", kv[0], kv[1]);
            }
            remaining_text = remaining_text.Trim(' ');
            args.Add("remaining_text", remaining_text);
            return args;
        }

        public static string removeFirstInstance(string sourceString, string removeString)
        {
            int index = sourceString.IndexOf(removeString, StringComparison.Ordinal);
            return (index < 0)
                ? sourceString
                : sourceString.Remove(index, removeString.Length);
        }
    }

    public class SignCommands
    {
        public List<SignCommand> commands;


        public SignCommands()
        {
            this.commands = new List<SignCommand>();
        }

        public void run_commands(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem instance, string message)
        {
            string command_name = message.Split(' ').FirstOrDefault();
            var command = this.commands.FirstOrDefault(i => i.name == command_name);
            if (command != null)
            {
                string result = command.run(instance);
                instance.Label.text = result;
            }
        }

        public void register_command(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, string> command_method)
        {
            commands.Add(new SignCommand(name, description, command_method));
        }

        public void register_command(SignCommand signCommand)
        {
            commands.Add((SignCommand)signCommand);
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem), "Awake")]
    public static class PatchAwake
    {
        static void Postfix(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem __instance)
        {
            Melon<Core>.Instance.labelledSurfaceItems.Add(__instance);
            Melon<Core>.Logger.Msg($"{__instance} added");
        }
    }
}