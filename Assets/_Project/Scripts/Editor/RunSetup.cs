using UnityEditor;

public class RunSetup
{
    [MenuItem("MundialRunner/Run Setup Now")]
    public static void Execute()
    {
        MundialRunnerSetup.SetupScene();
    }
}
