using UnityEngine;
using BunsKun.UI;

namespace BunsKun.Game
{
    /// <summary>
    /// Builds the entire playable game from code the moment Play begins - camera, UI,
    /// the run controller, and the first run. No scene setup, prefabs, or manual
    /// Inspector configuration is required; this runs in any scene automatically.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var cameraFollow = SetupCamera();
            UIFactory.EnsureEventSystem();

            var canvas = UIFactory.CreateCanvas("GameCanvas", 10);
            var canvasRoot = canvas.transform;

            var controllerGO = new GameObject("RunController");
            var runController = controllerGO.AddComponent<RunController>();
            runController.Bind(cameraFollow);

            var hud = controllerGO.AddComponent<HUDController>();
            hud.Initialize(runController, canvasRoot);

            var inventoryUI = controllerGO.AddComponent<InventoryUI>();
            inventoryUI.Initialize(runController, canvasRoot);

            var upgradeUI = controllerGO.AddComponent<UpgradeChoiceUI>();
            upgradeUI.Initialize(runController, canvasRoot);

            var endScreenUI = controllerGO.AddComponent<EndScreenUI>();
            endScreenUI.Initialize(runController, canvasRoot);

            runController.StartNewRun();
        }

        private static CameraFollow SetupCamera()
        {
            Camera cam = Camera.main;
            GameObject camGO;
            if (cam == null)
            {
                camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            else
            {
                camGO = cam.gameObject;
            }

            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10f);

            var follow = camGO.GetComponent<CameraFollow>();
            if (follow == null) follow = camGO.AddComponent<CameraFollow>();
            return follow;
        }
    }
}
