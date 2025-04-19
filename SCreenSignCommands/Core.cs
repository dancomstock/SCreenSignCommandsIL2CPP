using System.Collections;
using System.Text.RegularExpressions;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using static MelonLoader.MelonLogger;

[assembly: MelonInfo(typeof(SCreenSignCommandsIL2CPP.Core), "SCreenSignCommandsIL2CPP", "1.0.0", "animandan", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SCreenSignCommandsIL2CPP
{
    public class Core : MelonMod
    {
        public Queue<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem> labelledSurfaceItems;
        public SignCommands signCommands;
        public Dictionary<int, bool> runningLabels;
        public float waittime = 1f;
        public float innerwaittime = 0.1f;

        public override void OnInitializeMelon()
        {
            this.labelledSurfaceItems = new Queue<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem>();
            this.signCommands = new SignCommands();
            this.runningLabels = new Dictionary<int, bool>();
            LoggerInstance.Msg("Initialized.");
        }

        public void register_command(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, Task> command_method)
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

        //public void lockItems()
        //{

        //}

        public class Commander : MonoBehaviour
        {

            public void RunCommands()
            {
                //object routine = MelonCoroutines.Start(RunCommandsRoutine());
                var doit = Task.Run(() => (RunCommandsRoutine()));
            }

            //public IEnumerator waitForLoad()
            //{
            //    while (PlayerSingleton<AppsCanvas>.Instance == null)
            //    {
            //        yield return new WaitForSeconds(Melon<Core>.Instance.waittime);
            //    }
            //}

            public async Task RunCommandsRoutine()
            {
                
                while (PlayerSingleton<AppsCanvas>.Instance == null)
                {
                    //yield return new WaitForSeconds(Melon<Core>.Instance.waittime);
                    await Task.Delay(1000);
                    //yield return null;
                }

                var queue = Melon<Core>.Instance.labelledSurfaceItems;
                while (true)
                {
                    Melon<Core>.Logger.Msg($"Count = {queue.Count}");
                    if (queue.Count > 0)
                    {
                        //try
                        //{
                        var next = queue.Dequeue();
                        //Melon<Core>.Instance.runningLabels[next.GetInstanceID()] = "locked";
                        Melon<Core>.Instance.signCommands.run_commands(next);
                        //}
                        //catch (System.Exception ex)
                        //{
                        //    Melon<Core>.Logger.Msg($"error: {ex}");
                        //}

                    }
                    //yield return new WaitForSeconds(Melon<Core>.Instance.waittime);
                    await Task.Delay(1000);
                    //yield return null;
                }
            }
        }
    }

    public class SignCommand
    {
        public string name;
        public string description;
        public Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, Task> _command_function;


        public async Task run(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem instance)
        {
            try
            {
                Melon<Core>.Instance.runningLabels[instance.GetInstanceID()] = true;
                Melon<Core>.Logger.Msg($"{instance.GetInstanceID()} locked");
                await _command_function(instance);
            }
            catch (System.Exception ex)
            {
                Melon<Core>.Logger.Msg($"error: {ex}");
            }
            finally
            {
                Melon<Core>.Instance.runningLabels[instance.GetInstanceID()] = false;
                Melon<Core>.Logger.Msg($"{instance.GetInstanceID()} unlocked");
            }

            
        }

        public SignCommand(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, Task> command_function)
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

        public string run_commands(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem instance)
        {
            Melon<Core>.Logger.Msg($"In run commands with {instance.Message}");
            Melon<Core>.Logger.Msg($"Possible command {this.commands[0].name}");
            string command_name = instance.Message.Split(' ').FirstOrDefault();
            var command = this.commands.FirstOrDefault(i => i.name == command_name);
            if (command != null)
            {
                //IEnumerator result = command.run(instance);
                //async Task result = command.run(instance);
                SignCommand command_instance = new SignCommand(command.name, command.description, command._command_function);
                var doit = Task.Run(() => command_instance.run(instance));
                //instance.Label.text = result;
            }
            return "";
        }

        public void register_command(string name, string description, Func<Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem, Task> command_method)
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
            if (Melon<Core>.Instance.runningLabels.GetValueOrDefault(__instance.GetInstanceID(), false) != true)
            {
                Melon<Core>.Instance.labelledSurfaceItems.Enqueue(__instance);
                Melon<Core>.Logger.Msg($"{__instance} added");
            }
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem), "MessageSubmitted")]
    public static class PatchMessageSubmitted
    {
        static void Postfix(Il2CppScheduleOne.EntityFramework.LabelledSurfaceItem __instance)
        {
            //__instance.GetInstanceID();
            if(Melon<Core>.Instance.runningLabels.GetValueOrDefault(__instance.GetInstanceID(), false) != true)
            {
                Melon<Core>.Instance.labelledSurfaceItems.Enqueue(__instance);
                Melon<Core>.Logger.Msg($"{__instance} added");
            }
        }
    }
}