using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        public GameObject buttonsHolder;
        public TextMeshProUGUI joinCodeText;
        public TextMeshProUGUI joinCodeInput;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                if (GUILayout.Button("Host"))
                {

                    networkManager.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    networkManager.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    networkManager.StartServer();
                }
            }

            GUILayout.EndArea();
        }

        public async void CreateRelay()
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(3);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                joinCodeText.text = joinCode;

                var rsd = new RelayServerData(allocation, "dtls");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(rsd);
                NetworkManager.Singleton.StartHost();

                buttonsHolder.SetActive(false);
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void JoinRelay()
        {
            try
            {
                var joinCode = joinCodeInput.text;

                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var rsd = new RelayServerData(allocation, "dtls");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(rsd);
                NetworkManager.Singleton.StartClient();

                buttonsHolder.SetActive(false);
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
}
