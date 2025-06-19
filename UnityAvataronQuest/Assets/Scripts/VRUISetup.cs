using System.Collections.Generic;
//using Meta.XR.Samples;
//using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.StartScene
{
    public class VRUISetup : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_uiHelpersToInstantiate = null;

        [SerializeField]
        private List<GameObject> m_toEnable = null;

        [SerializeField]
        private List<GameObject> m_toDisable = null;
        public static VRUISetup Instance;

        public delegate void OnClick();

        public delegate void OnToggleValueChange(Toggle t);

        public delegate void OnSlider(float f);

        public delegate bool ActiveUpdate();

        //private OVRCameraRig m_rig;
        //private Dictionary<string, ToggleGroup> m_radioGroups = new();
        private LaserPointer m_lp;
        private LineRenderer m_lr;

        public LaserPointer.LaserBeamBehaviorEnum LaserBeamBehavior = LaserPointer.LaserBeamBehaviorEnum.OnWhenHitTarget;
        public bool IsHorizontal = false;
        public bool UsePanelCentricRelayout = false;

        public void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            gameObject.SetActive(false);
            // m_rig = FindFirstObjectByType<OVRCameraRig>();
            for (var i = 0; i < m_toEnable.Count; ++i)
            {
                m_toEnable[i].SetActive(false);
            }

            if (m_uiHelpersToInstantiate)
            {
                _ = Instantiate(m_uiHelpersToInstantiate);
            }

            m_lp = FindFirstObjectByType<LaserPointer>();
            if (!m_lp)
            {
                Debug.LogError("Debug UI requires use of a LaserPointer and will not function without it. " +
                            "Add one to your scene, or assign the UIHelpers prefab to the DebugUIBuilder in the inspector.");
                return;
            }

            m_lp.LaserBeamBehavior = LaserBeamBehavior;

            if (!m_toEnable.Contains(m_lp.gameObject))
            {
                m_toEnable.Add(m_lp.gameObject);
            }

            GetComponent<OVRRaycaster>().pointer = m_lp.gameObject;
            m_lp.gameObject.SetActive(false);
        }

    }
}