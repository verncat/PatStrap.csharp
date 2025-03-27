using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Evergine.Bindings.OpenXR;

namespace OpenXR;

class Program
{
    static void CheckResult(XrResult result, string errorText = "", [CallerArgumentExpression(nameof(result))] string callerArgName = "")
    {
        Debug.Assert(
            result == XrResult.XR_SUCCESS, 
            $"Error {result.ToString()} in {callerArgName}{(errorText.Length > 0 ? ": " + errorText : string.Empty)}"
        );
    }
    static unsafe void Main(string[] args)
    {

        XrInstance instance = new XrInstance();
        
        try
        {
            var applicationName = "applicationNameBytes";
            var engineName = "OpenXR Engine";

            var applicationInfo = new XrApplicationInfo
            {
                applicationVersion = 1,
                engineVersion = 1,
                apiVersion = 0x100000000002b
            };
            fixed (char* applicationNamePtr = applicationName,
                   engineNamePtr = engineName)
            {
                Encoding.UTF8.GetBytes(applicationNamePtr, applicationName.Length, applicationInfo.applicationName,
                    128);
                Encoding.UTF8.GetBytes(engineNamePtr, applicationName.Length, applicationInfo.engineName, 128);
            }


            var instanceCreateInfo = new XrInstanceCreateInfo()
            {
                type = XrStructureType.XR_TYPE_INSTANCE_CREATE_INFO,
                applicationInfo = applicationInfo,
                enabledExtensionCount = 0,
                enabledExtensionNames = null,
                createFlags = 0
            };

            CheckResult(OpenXRNative.xrCreateInstance(&instanceCreateInfo, &instance));

            var systemGetInfo = new XrSystemGetInfo
            {
                type = XrStructureType.XR_TYPE_SYSTEM_GET_INFO,
                formFactor = XrFormFactor.XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY,
            };

            ulong systemId = 0;
            do
            {
                CheckResult(OpenXRNative.xrGetSystem(instance, &systemGetInfo, &systemId));
                Console.WriteLine($"Detect system id: {systemId}");
            } while (systemGetInfo.next != null);

            var instanceProperties = new XrInstanceProperties();
            

            // XrGraphicsBindingD3D11KHR binding = { XR_TYPE_GRAPHICS_BINDING_D3D11_KHR };
            // binding.device = d3d_device;

            // XrSessionCreateInfo sessionCreateInfo = {};
            // sessionCreateInfo.type = XR_TYPE_SESSION_CREATE_INFO;
            // sessionCreateInfo.systemId = systemId;
            //
            // XrSession session;
            // result = xrCreateSession(instance, &sessionCreateInfo, &session);
            // checkResult(result, "Failed to create OpenXR session");

            var sessionCreateInfo = new XrSessionCreateInfo()
            {
                type = XrStructureType.XR_TYPE_SESSION_CREATE_INFO,
                systemId = systemId,
            };

            XrSession session;
            CheckResult(OpenXRNative.xrCreateSession(instance, &sessionCreateInfo, &session));


            // fixed (byte* actionName = "my_haptic"u8,
            //        localizedActionName = "My test haptic"u8)
            // {
            //     var actionCreateInfo = new XrActionCreateInfo
            //     {
            //         type = XrStructureType.XR_TYPE_ACTION_CREATE_INFO,
            //         actionType = XrActionType.XR_ACTION_TYPE_VIBRATION_OUTPUT,
            //         countSubactionPaths = 1,
            //         actionName = actionName,
            //         localizedActionName = localizedActionName,
            //     };
            // }
        }
        finally
        {
            CheckResult(OpenXRNative.xrDestroyInstance(instance));
        }
    }
}