﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Media;

namespace CH.Alika.POS.Hardware
{
    
    public class MMMDocumentScanner : IScanSource
    {
        private MMM.Readers.Modules.Swipe.SwipeSettings swipeSettings;
        public event EventHandler<CodeLineScanEvent> OnCodeLineScanEvent;
        public void Activate()
        {
            // Initialise logging and error handling first. The error handler callback
            // will receive all error messages generated by the 3M Page Reader SDK
            MMM.Readers.Modules.Reader.SetErrorHandler(
                new MMM.Readers.ErrorDelegate(DeviceErrorHandler),
                IntPtr.Zero
            );

            InitalizeLogging();
            LoadSwipeSettings(ref swipeSettings);
            InitializeSwipeReader(
                swipeSettings, 
                new MMM.Readers.Modules.Swipe.DataDelegate(DeviceDataHandler),
                new MMM.Readers.FullPage.EventDelegate(DeviceEventHanlder));
            
        }

        public override String ToString() 
        {
            // Display the hardware device and protocol in use
            string lProtocolName = new string(swipeSettings.Protocol.ProtocolName).Replace("\0","");

            if (lProtocolName.StartsWith("RTE"))
            {
                // For RTE_INTERRUPT and RTE_POLLED modes, the Swipe Reader API can 
                // automatically send Enable Device commands once finished reading so
                // that you do not have to
                if (
                    !lProtocolName.Equals("RTE_NATIVE") &&
                    swipeSettings.Protocol.RTE.AutoSendEnableDevice > 0
                )
                {
                    lProtocolName = string.Concat(
                        lProtocolName,
                        ", Auto Send Enable Command"
                    );
                }

                if (swipeSettings.Protocol.RTE.UseBCC > 0)
                {
                    lProtocolName = string.Concat(
                        lProtocolName,
                        ", with BCC"
                    );
                }
                else
                {
                    lProtocolName = string.Concat(
                        lProtocolName,
                        ", no BCC"
                    );
                }
            }

            return string.Format(
                "MMMDocumentScanner ProtocolSettings[{0}]",
                lProtocolName
            );
        }

        private void InitializeSwipeReader(
            MMM.Readers.Modules.Swipe.SwipeSettings swipeSettings,
            MMM.Readers.Modules.Swipe.DataDelegate dataDelegate,
            MMM.Readers.FullPage.EventDelegate eventDelegate
            )
        {
            MMM.Readers.ErrorCode lErrorCode = MMM.Readers.Modules.Swipe.Initialise(
                    swipeSettings,
                    dataDelegate,
                    eventDelegate);
            if (lErrorCode != MMM.Readers.ErrorCode.NO_ERROR_OCCURRED) 
            {
                String message = String.Format("Failed SwipeReader Initialization {0} {1}", (int)lErrorCode,lErrorCode.ToString());
                throw new PosHardwareException(message);
            }
        }

        private void LoadSwipeSettings(ref MMM.Readers.Modules.Swipe.SwipeSettings swipeSettings)
        {
            MMM.Readers.ErrorCode lErrorCode = MMM.Readers.Modules.Reader.LoadSwipeSettings(
                    ref swipeSettings
                );

            if (lErrorCode != MMM.Readers.ErrorCode.NO_ERROR_OCCURRED) 
            {
                String message = String.Format("Failed to Load Swipe Settings {0} {1}", (int)lErrorCode,lErrorCode.ToString());
                throw new PosHardwareException(message);
            }
            
            
        }

        private void InitalizeLogging()
        {
            MMM.Readers.ErrorCode lErrorCode = MMM.Readers.Modules.Reader.InitialiseLogging(
                true,
                3,
                -1,
                "SwipeReader.Net.log"
            );

            if (lErrorCode != MMM.Readers.ErrorCode.NO_ERROR_OCCURRED) 
            {
                String message = String.Format("Failed Logger Initialization {0} {1}", (int)lErrorCode,lErrorCode.ToString());
                throw new PosHardwareException(message);
            }
            
        }

        private void DeviceErrorHandler(MMM.Readers.ErrorCode aErrorCode, string aErrorMessage)
        {
            Console.WriteLine("***READER ERROR: " + aErrorCode);
        }

        private void DeviceDataHandler(MMM.Readers.Modules.Swipe.SwipeItem aDataItem, object aData) 
        {
            if (aDataItem == MMM.Readers.Modules.Swipe.SwipeItem.OCR_CODELINE)
            {
                MMM.Readers.CodelineData codeLineData = (MMM.Readers.CodelineData)aData;
                if (codeLineData.CheckDigitDataListCount > 0
                    && (codeLineData.CodelineValidationResult == MMM.Readers.CheckDigitResult.CDR_Valid
                    || codeLineData.CodelineValidationResult == MMM.Readers.CheckDigitResult.CDR_Warning)
                    )
                {
                    OnCodeLineScanEvent(this, new CodeLineScanEvent(codeLineData));
                    SystemSounds.Exclamation.Play();
                }
                else
                {
                    SystemSounds.Hand.Play();
                }
            }
        }

        private void DeviceEventHanlder(MMM.Readers.FullPage.EventCode aEventType)
        {
            // Console.WriteLine("***READER EVENT***");
        }

        public void Dispose()
        {
            MMM.Readers.Modules.Swipe.Shutdown();
        }

        

    }
}
