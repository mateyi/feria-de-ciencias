using UnityEditor;
using UnityEngine;

public class FrameFullTrack
{
    public static void Execute()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        // Frame from behind and above, looking down the track
        sv.pivot     = new Vector3(0, 0, 60f);
        sv.rotation  = Quaternion.Euler(35f, 0f, 0f);
        sv.size      = 35f;
        sv.orthographic = false;
        sv.Repaint();
    }
}
