// <copyright file="Main.cs" company="CNC Software, LLC.">
// Copyright (c) 2023 - All rights reserved.
// </copyright>
// <summary>
//  Implements the NetHook3App interface
//
//  If this project is helpful please take a short survey at ->
//  https://survey.alchemer.com/s3/6799376/Mastercam-API-Developer-Satisfaction-Survey
// </summary>

namespace MastercamDriver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml;
    using Cnc.Tool.Interop;
    using Mastercam.App;
    using Mastercam.App.Types;
    using Mastercam.IO;
    using Mastercam.IO.Types;
    using Mastercam.Support.UI;



    /// <summary> A class that implements the NetHook3App interface. </summary>
    public class Main : NetHook3App
    {
        #region Methods exposed via the Function Table

        /// <summary> My ribbon group one. </summary>
        ///
        /// <remarks> For a &lt;Group&gt; this is needed to obtains the "name" of the Group
        ///           from the string Resources assigned to this method in the FT file. </remarks>
        ///
        /// <param name="param"> The parameter (not used). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn MyRibbonGroupOne(int param)
        {
            MessageBox.Show(Properties.Resources.RIBBON_ONE_GROUP);
            return MCamReturn.NoErrors;
        }

        /// <summary> My ribbon group two. </summary>
        ///
        /// <remarks> For a &lt;Group&gt; this is needed to obtains the "name" of the Group
        ///           from the string Resources assigned to this method in the FT file. </remarks>
        ///
        /// <param name="param"> The parameter (not used). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn MyRibbonGroupTwo(int param)
        {
            MessageBox.Show(Properties.Resources.RIBBON_TWO_GROUP);
            return MCamReturn.NoErrors;
        }

        /// <summary> Group one launcher. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn GroupOneLauncher(int param)
        {
            MessageBox.Show(Properties.Resources.RIBBON_ONE_GROUP_LAUNCHER);
            return MCamReturn.NoErrors;
        }

        /// <summary> Command one. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn StartAddIn(int param)
        {
            //MessageBox.Show(Properties.Resources.COMMAND_ONE);
            var winView = new MainView { TopLevel = true };

            //Set the dialog as modeless to Mastercam, always on top
            var handle = Control.FromHandle(MastercamWindow.GetHandle().Handle);
            _ = new ModelessDialogTabsHandler(winView);

            winView.StartPosition = FormStartPosition.CenterParent;
            winView.Show(handle);
            //generateTool();
            return MCamReturn.NoErrors;
        }

        /// <summary> Command two. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn CommandTwo(int param)
        {
            MessageBox.Show(Properties.Resources.COMMAND_TWO);
            return MCamReturn.NoErrors;
        }

        /// <summary> Command three. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn CommandThree(int param)
        {
            MessageBox.Show(Properties.Resources.COMMAND_THREE);
            return MCamReturn.NoErrors;
        }

        /// <summary> My ribbon gallery. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn MyRibbonGallery(int param)
        {
            MessageBox.Show(Properties.Resources.RIBBON_GALLERY);
            return MCamReturn.NoErrors;
        }

        /// <summary> Edits command. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn Edit(int param)
        {
            MessageBox.Show(Properties.Resources.EDIT);
            return MCamReturn.NoErrors;
        }

        /// <summary> Delete command. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A Mastercam.App.Types.MCamReturn. </returns>
        public MCamReturn Delete(int param)
        {
            MessageBox.Show(Properties.Resources.DELETE);
            return MCamReturn.NoErrors;
        }

        #endregion Methods exposed via the Function Table


        /// <summary> This method serves as the main entry point for the NET-Hook Add-In. </summary>
        ///
        /// <param name="param"> The parameter (optional). </param>
        ///
        /// <returns> A MCamReturn return type code. </returns>
        public override MCamReturn Run(int param)
        {
            // All of this add-in's functionality is being done via Function Table (FT) type commands.
            return MCamReturn.NoErrors;
        }

        /// <summary> Initialize anything we need for the NET-Hook here. </summary>
        ///
        /// <param name="param"> System parameter. </param>
        ///
        /// <returns> A <c>MCamReturn</c> return type representing the outcome of your NetHook application. </returns>
        public override MCamReturn Init(int param)
        {
            // true  = we are going to get the XML by reading in an external file.
            // false = we are going to retrieve the XML from the resources embedded in this Assembly.
            if (!this.CreateCustomRibbonTab(false))
            {
                MessageBox.Show(
                                Properties.Messages.InitializationFailedMessage,
                                Properties.Messages.ErrorMessage,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

               EventManager.LogEvent(
                                    MessageSeverityType.WarningMessage,
                                    Properties.Resources.Title,
                                    Properties.Messages.InsertThirdPartyRibbonTabsFailed);
            }

            return MCamReturn.NoErrors;
        }

        /// <summary> Executes the initialization of the custom Ribbon Tab UI. </summary>
        ///
        /// <param name="readFromFile"> True to read the XML data from an external file,
        ///                             false to retrieve the XML from a resource embedded in this Assembly. </param>
        ///
        /// <returns> True if it succeeds, false if it fails. </returns>
        private bool CreateCustomRibbonTab(bool readFromFile)
        {
            var xmlData = "RibbonCustomization.xml";

            if (readFromFile)
            {
                xmlData = SettingsManager.UserDirectory + @"data\" + xmlData;

                EventManager.LogEvent(
                                     MessageSeverityType.InformationalMessage,
                                     Properties.Resources.Title, 
                                     Properties.Messages.MessageXmlData);                
            }           

            var xml = this.LoadXml(xmlData, readFromFile);

            // Ask Mastercam to create the custom Ribbon Tab additions. 
            if (!MastercamRibbon.InsertThirdPartyRibbonTabs(xml))
            {
                MessageBox.Show(Properties.Messages.InsertThirdPartyRibbonTabsFalse);
                return false;
            }

            return true;
        }

        /// <summary> Loads the XML data that defines our Ribbon Tab. </summary>
        ///
        /// <param name="xmlData">  The name of the XML data (full file path or resource name). </param>
        /// <param name="readFile"> True to read the XML data from an external file, 
        ///                         false to retrieve the XML from a resource embedded in this Assembly. </param>
        ///
        /// <returns> The XML data to feed to InsertThirdPartyRibbonTabs(xml). </returns>
        private string LoadXml(string xmlData, bool readFile)
        {
            // Get the XML data from an external file ?
            if (readFile)
            {                              
                try
                {
                    var doc = new XmlDocument();
                    doc.Load(xmlData);
                    return doc.OuterXml;
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                                    Properties.Messages.NotLoadedXmlResource + e.Message,
                                    Properties.Messages.ErrorMessage,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }
            else
            {
                // Get the XML data from the Resources of this Assembly
                var rn = string.Empty;
                var assembly = Assembly.GetExecutingAssembly();
                foreach (var name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith(xmlData, StringComparison.OrdinalIgnoreCase))
                    {
                        rn = name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(rn))
                {
                    using (var stream = assembly.GetManifestResourceStream(rn))
                    {
                        try
                        {
                            var doc = new XmlDocument();
                            doc.Load(stream);
                            return doc.OuterXml;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(
                                            Properties.Messages.NotLoadedXmlResource + e.Message,
                                            Properties.Messages.ErrorMessage,
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                        }
                    }
                }
            }

            return string.Empty;
        }

    }
}