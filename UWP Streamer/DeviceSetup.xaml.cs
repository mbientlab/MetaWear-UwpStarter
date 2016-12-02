using MbientLab.MetaWear.Peripheral;
using static MbientLab.MetaWear.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

// Information for Stream Sockects in UWP
// https://msdn.microsoft.com/en-us/library/windows/apps/xaml/jj150599
// http://donatas.xyz/streamsocket-tcpip-client.html


using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MbientLab.MetaWear.Template {
    /// <summary>
    /// Blank page where users add their MetaWear commands
    /// </summary>
    /// 

   


    public sealed partial class DeviceSetup : Page {
        /// <summary>
        /// Pointer representing the MblMwMetaWearBoard struct created by the C++ API
        /// </summary>
        private IntPtr cppBoard;

        private static StreamSocket clientSocket;

        public DeviceSetup() {
            this.InitializeComponent();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            var mwBoard= MetaWearBoard.getMetaWearBoardInstance(e.Parameter as BluetoothLEDevice);
            cppBoard = mwBoard.cppBoard;

            // cppBoard is initialized at this point and can be used
        }

        /// <summary>
        /// Callback for the back button which tears down the board and navigates back to the <see cref="MainPage"/> page
        /// </summary>
        private void back_Click(object sender, RoutedEventArgs e) {
            mbl_mw_metawearboard_tear_down(cppBoard);

            this.Frame.Navigate(typeof(MainPage));
        }
       
        private Core.Fn_IntPtr accDataHandler = new Core.Fn_IntPtr(async dataPtr => {
            var marshalledData = Marshal.PtrToStructure<Core.Data>(dataPtr);
            Core.CartesianFloat acc_value = Marshal.PtrToStructure<Core.CartesianFloat>(marshalledData.value);
            System.Diagnostics.Debug.WriteLine("Acceleartion= " + acc_value);
            // Send Acceleration value on TCP connection
            DataWriter writer = new DataWriter(clientSocket.OutputStream);
            //string sendData = "Ray";
            //writer.WriteString(sendData);
            writer.WriteSingle(16); // 4 singles, one for type and 3 for XYZ
            writer.WriteSingle(1); // 1 means Acceleration package
            writer.WriteSingle(acc_value.x);
            writer.WriteSingle(acc_value.y);
            writer.WriteSingle(acc_value.z);
            await writer.StoreAsync();
            await writer.FlushAsync();
            // In order to prolong the lifetime of the stream, detach it from the DataWriter
            writer.DetachStream();
        });

        private async void accStart_Click(object sender, RoutedEventArgs e)
        {
            // Connect to Server at 127.0.0.1:40000
            //StreamSocket clientSocket = new StreamSocket();
            clientSocket = new StreamSocket(); //TCP comm
            HostName serverHost = new HostName("127.0.0.1");
            await clientSocket.ConnectAsync(serverHost, "40000");           



            IntPtr accSignal = mbl_mw_acc_get_acceleration_data_signal(cppBoard);

            mbl_mw_datasignal_subscribe(accSignal, accDataHandler);
            mbl_mw_acc_enable_acceleration_sampling(cppBoard);
            mbl_mw_acc_start(cppBoard);
        }

        private void accStop_Click(object sender, RoutedEventArgs e)
        {
            IntPtr accSignal = mbl_mw_acc_get_acceleration_data_signal(cppBoard);

            mbl_mw_acc_stop(cppBoard);
            mbl_mw_acc_disable_acceleration_sampling(cppBoard);
            mbl_mw_datasignal_unsubscribe(accSignal);

            clientSocket.Dispose();

        }

    }
}
