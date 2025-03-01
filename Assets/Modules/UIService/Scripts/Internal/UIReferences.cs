using UnityEngine;
using UnityEngine.UI;

namespace Modules.UIService.Internal
{ 
    public class UIReferences : MonoBehaviour
    {
        public GameObject MainScreen;
        public GameObject WelcomeScreen;

        public Image EndCall;
        public Image VideoCall;
        public Image Mic;
        public Image Minimize;
        public Image ThoughtEmoji;
        public Image MicEmoji;
        public Image ThinkCloud;

        public Button StartButton;
        
        // Camera view components
        public RawImage CameraView;
        public AspectRatioFitter CameraAspectRatioFitter;
        public GameObject CameraViewContainer;
    }
}