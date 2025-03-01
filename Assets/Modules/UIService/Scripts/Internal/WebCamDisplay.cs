using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;  // Add this for Android Permission class

namespace Modules.UIService.Internal
{
    public class WebCamDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage displayImage;
        [SerializeField] private AspectRatioFitter aspectRatioFitter;
        [SerializeField] private bool frontFacing = true;
        [SerializeField] private int preferredWidth = 1280;
        [SerializeField] private int preferredHeight = 720;
        [SerializeField] private int preferredFPS = 30;
        
        private WebCamTexture webCamTexture;
        private bool isCameraActive = false;
        private bool isInitialized = false;
        
        private void OnDestroy()
        {
            StopCamera();
        }
        
        private void Start()
        {
            #if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                return;
            }
            #endif
            
            // Rest of your webcam initialization code...
        }
        
        public void InitializeWebCam()
        {
            // Don't try to initialize if already initialized
            if (isInitialized) return;
            
            // Find the display image if not assigned
            if (displayImage == null)
            {
                // Try to find it in the scene
                displayImage = FindRawImageInScene();
                
                if (displayImage == null)
                {
                    Debug.LogWarning("WebCamDisplay: No RawImage found for display. Will try again when camera is toggled.");
                    return;
                }
            }
            
            // Find the aspect ratio fitter if not assigned
            if (aspectRatioFitter == null && displayImage != null)
            {
                aspectRatioFitter = displayImage.GetComponent<AspectRatioFitter>();
            }
            
            // Get available devices
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("WebCamDisplay: No camera devices found");
                return;
            }
            
            // Find appropriate camera (front or back)
            WebCamDevice selectedDevice = devices[0]; // Default to first device
            
            for (int i = 0; i < devices.Length; i++)
            {
                // On mobile, check for front/back camera as requested
                #if UNITY_ANDROID || UNITY_IOS
                if (devices[i].isFrontFacing == frontFacing)
                {
                    selectedDevice = devices[i];
                    break;
                }
                #else
                // On desktop, just use the first camera
                selectedDevice = devices[i];
                break;
                #endif
            }
            
            // Create the WebCamTexture
            webCamTexture = new WebCamTexture(
                selectedDevice.name,
                preferredWidth,
                preferredHeight,
                preferredFPS
            );
            
            // Assign to the RawImage
            displayImage.texture = webCamTexture;
            displayImage.material.mainTexture = webCamTexture;
            
            isInitialized = true;
            Debug.Log("WebCamDisplay: Successfully initialized camera");
        }
        
        private RawImage FindRawImageInScene()
        {
            // First try to find in UIReferences
            var uiReferences = FindObjectOfType<UIReferences>();
            if (uiReferences != null && uiReferences.CameraView != null)
            {
                return uiReferences.CameraView;
            }
            
            // If not found in UIReferences, try to find any RawImage in a CameraViewContainer
            var containers = GameObject.FindGameObjectsWithTag("CameraViewContainer");
            foreach (var container in containers)
            {
                var rawImage = container.GetComponentInChildren<RawImage>();
                if (rawImage != null)
                {
                    return rawImage;
                }
            }
            
            // Last resort: find any RawImage
            return FindObjectOfType<RawImage>();
        }
        
        public void StartCamera()
        {
            // Make sure we're initialized before starting
            if (!isInitialized)
            {
                InitializeWebCam();
                
                // If initialization failed, exit
                if (!isInitialized) return;
            }
            
            if (webCamTexture != null && !webCamTexture.isPlaying)
            {
                webCamTexture.Play();
                isCameraActive = true;
                
                // Start a coroutine to update the aspect ratio once the camera is initialized
                StartCoroutine(UpdateAspectRatio());
            }
        }
        
        public void StopCamera()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
                isCameraActive = false;
            }
        }
        
        public void ToggleCamera()
        {
            if (isCameraActive)
            {
                StopCamera();
            }
            else
            {
                StartCamera();
            }
        }
        
        private System.Collections.IEnumerator UpdateAspectRatio()
        {
            // Wait until the camera has started and has a valid size
            yield return new WaitUntil(() => webCamTexture.width > 100);
            
            // Update the aspect ratio
            if (aspectRatioFitter != null)
            {
                float aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
                aspectRatioFitter.aspectRatio = aspectRatio;
            }
            
            // Handle rotation based on device orientation
            #if UNITY_ANDROID
            int angle = -webCamTexture.videoRotationAngle;
            // Add 180 degrees to flip the image right side up for Android
            angle += 180;
            displayImage.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            #else
            displayImage.rectTransform.localEulerAngles = new Vector3(0, 0, -webCamTexture.videoRotationAngle);
            #endif
            
            // Handle mirroring for front-facing camera
            if (frontFacing)
            {
                displayImage.rectTransform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                displayImage.rectTransform.localScale = new Vector3(1, 1, 1);
            }
        }
    }
} 