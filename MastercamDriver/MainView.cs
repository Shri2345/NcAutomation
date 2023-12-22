using Cnc.Tool.Interop;
using Mastercam.IO;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MastercamDriver
{
    public partial class MainView : Form
    {
        string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        public MainView()
        {
            InitializeComponent();
            InitWebView();
        }
        public async void InitWebView()
        {
            //await WebView.EnsureCoreWebView2Async();
            //Testing the source control for changes. In our case, we use git.
            string MachineTempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine), AssemblyName);

            await InitializeCoreWebView2Async(webView21, MachineTempPath);
            //WebView.CoreWebView2.Navigate("https://www.google.com/");
            webView21.CoreWebView2.Navigate("http://localhost:3000/");
        }
        private async Task InitializeCoreWebView2Async(Microsoft.Web.WebView2.WinForms.WebView2 webView, string userDataFolder = null)
        {
            //initialize CoreWebView2 
            CoreWebView2EnvironmentOptions options = null;
            CoreWebView2Environment cwv2Environment = null;

            //it's recommended to create the userDataFolder in the same location
            //that your other application data is stored (ie: in a folder in %APPDATA%)
            //if not specified, we'll create a folder in %TEMP%
            if (string.IsNullOrEmpty(userDataFolder))
                userDataFolder = Path.Combine(Path.GetTempPath(), AssemblyName);

            //create WebView2 Environment using the installed or specified WebView2 Runtime version.
            //cwv2Environment = await CoreWebView2Environment.CreateAsync(@"C:\Program Files (x86)\Microsoft\Edge Dev\Application\1.0.1054.31", userDataFolder, options);
            cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

            //initialize
            await webView.EnsureCoreWebView2Async(cwv2Environment);            
        }
        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string dataToSend = "Hello from C#";
            await webView21.CoreWebView2.ExecuteScriptAsync($"addProducts('{dataToSend}')");
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            switch (e.TryGetWebMessageAsString())
            {
                case "Create Block":
                    //CreateObject();
                    break;
                case "Hole Feature Recognize":
                    //ReadHoleData();
                    //AutoDataRecognize();
                    break;
                case "Generate Tool":
                    generateTool();
                    //GenerateToolPath();
                    break;
                default:
                    break;
            }
        }


        private void generateTool()
        {
            var result = false;

            var folder = System.IO.Path.Combine(SettingsManager.SharedDirectory, @"mill\Tools\");
            var library = "My End Mill Library_Test1.tooldb";

            // If the library does not exist, it will be created.
            var toolLibrarySystem = new ToolLibrarySystem();
            toolLibrarySystem.OpenLibrary(folder + library, true);

            // Create mill tool.
            var toolAssembly = this.DemonstrateCreatingFlatEndMillToolAssembly();
            if (toolAssembly != null)
            {
                if (toolLibrarySystem.IsOpen())
                {
                    result = toolLibrarySystem.Add(toolAssembly);
                }
               
                //MessageBox.Show(
                //        Properties.Resources.MessageAddToolAssemblyToLibrary + result,
                //        Properties.Resources.LabelToolLibrary,
                //        MessageBoxButtons.OK,
                //        MessageBoxIcon.Information);
            }

            // Create a tool holder
            var holder = this.DemonstrateCreatingToolHolder();
            if (holder != null)
            {
                if (toolLibrarySystem.IsOpen())
                {
                    result = toolLibrarySystem.Add(holder);
                }

                //MessageBox.Show(
                //        Properties.Resources.MessageAddHolderToLibrary + result,
                //        Properties.Resources.LabelToolLibrary,
                //        MessageBoxButtons.OK,
                //        MessageBoxIcon.Information);
            }

            // Create the default tool holder
            holder = this.DemonstrateCreatingToolHolder(true);
            if (holder != null)
            {
                if (toolLibrarySystem.IsOpen())
                {
                    result = toolLibrarySystem.Add(holder);
                }

                //MessageBox.Show(
                //   Properties.Resources.MessageAddDefaultHolderToLibrary + result,
                //    Properties.Resources.LabelToolLibrary,
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information);
            }

            // Now change the Holder in the Tool Assembly
            if (result)
            {
                var newHolder = this.FindHolder(folder + library, "My Custom Holder");
                if (newHolder != null)
                {
                    result = this.ChangeHolder(ref toolAssembly, newHolder);
                    if (result)
                    {
                        toolLibrarySystem.OpenLibrary(folder + library, true);
                        if (toolLibrarySystem.IsOpen())
                        {
                            // If adding this as a new tool assembly -
                            // toolAssembly.ResetIDs();
                            // result = tls.Add(toolAssembly);
                            result = toolLibrarySystem.Update(toolAssembly);
                        }
                    }
                }

                //MessageBox.Show(
                //        Properties.Resources.MessageAddDefaultHolderToLibrary + result,
                //        Properties.Resources.LabelToolLibrary,
                //        MessageBoxButtons.OK,
                //        MessageBoxIcon.Information);
            }
            MessageBox.Show("Tool generation is completed.");
            // return MCamReturn.NoErrors;
        }

        /// <summary> Change the holder in a tool assembly. </summary>
        /// 
        /// <param name="assembly"> [in/out] The assembly. </param>
        /// <param name="holder"> The holder. </param>
        /// 
        /// <returns> True if it succeeds, false if it fails. </returns>
        private bool ChangeHolder(ref TlAssembly assembly, TlHolder holder)
        {
            if (holder != null)
            {
                holder.ResetIDs();
                assembly.MainHolder = holder;
                var success = assembly.SetAsDefaultAssembly(false);

                Debug.Assert(success, "Assembly is still set as a default assembly");
                return success;
            }

            return false;
        }

        /// <summary> Searches for the first holder with the matching holder name. </summary>
        /// 
        /// <param name="libName"> The (full path) name of the library. </param>
        /// <param name="holderName"> The name of the holder to search for. </param>
        /// 
        /// <returns>The found holder. </returns>
        private TlHolder FindHolder(string libName, string holderName)
        {
            var toolLibrary = new ToolLibrarySystem();
            toolLibrary.OpenLibrary(libName, true);

            if (toolLibrary.IsOpen())
            {
                var holders = new List<TlHolder>();
                toolLibrary.GetAllTlHolders(holders);

                foreach (var holder in holders)
                {
                    if (holder.Name == holderName)
                    {
                        toolLibrary.CloseLibrary();
                        return holder;
                    }
                }

                toolLibrary.CloseLibrary();
            }

            return null;
        }

        /// <summary> Demonstrate creating a tool holder. </summary>
        /// 
        /// <param name="defaultHolder"> (Optional) True to create just a default holder. </param>
        /// 
        /// <returns> The new Holder. </returns>
        private TlHolder DemonstrateCreatingToolHolder(bool defaultHolder = false)
        {
            // Create a default tool assembly of the specified type
            var holder = TlServices.CreateITlAssemblyItemFactory().CreateMillHolder(false);

            // Add segment(s) to the default holder that was created
            if (!defaultHolder)
            {
                holder.Name = "My Custom Holder";

                // TopWidth, BottomWidth, Height
                holder.AddSegment(3, 3, 1);
                holder.AddSegment(3, 2, 0.5);
                holder.AddSegment(2, 1, 0.25);
            }

            return holder;
        }

        /// <summary> Demonstrate creating a high feed mill tool assembly. </summary>
        /// 
        /// <returns> The new assembly </returns>
        private TlAssembly DemonstrateCreatingFlatEndMillToolAssembly()
        {
            var assembly = TlServices.CreateITlAssemblyFactory().Create(MCToolType.FlatEndmill, false);
            assembly.Name = "Programmatically created Assembly";

            var millTool = assembly.GetMillTool();

            var tool = millTool as TlToolEndmill;
            Debug.Assert(tool != null, "Cast to TlToolEndmill type was invalid!");

            tool.ToolNumber = 1;
            tool.Name = "My End Mill Tool";

            // If you are changing the "length" of the tool ->
            // It is best to update both the OverallLength and TruePhysicalLength properties.
            // Setting them apart will tell the containing assembly that the projection of that tool
            // is whatever is in the overall length property.
            // If the overall length is set to be greater than the current true physical length,
            // the true physical length is updated to match the overall length value.

            // Set the Overall dimensions
            tool.OverallDiameter = 0.9125; // Cutting diameter
            tool.TruePhysicalLength = tool.OverallLength = 4.125; // Overall length
            tool.CuttingDepth = 0.735; // Flute Length / Cutting Length

            // Set the Cutting geometry dimensions
            tool.TipDiameter = 0.505;

            // Set the Non-cutting geometry dimensions
            tool.ShoulderLength = 1.15; // Shoulder length
            tool.ShoulderDiameter = 0.5; // Shoulder diameter
            tool.ArborDiameter = 0.75; // Shank diameter

            // Set the Shank Type
            tool.ShankType = TlShankType.Straight; // Straight / Tapered / Reduced            

            // tool.ShankType = TlShankType.Tapered; // Straight / Tapered / Reduced
            // tool.TaperAngle = 45.0;
            //// or...
            // tool.TaperLength = 0.135;

            // tool.ShankType = TlShankType.Reduced; // Straight / Tapered / Reduced
            // tool.NeckDiameter = 0.5; // Neck Diameter
            // tool.TaperLength = 0.575; // Neck length

            // Set Feed/Speed parameters
            var opParams = tool.OpParams;
            opParams.FeedRate = 7.55;
            opParams.PlungeRate = 4.4;
            opParams.RetractRate = 13.5;
            opParams.SpindleSpeed = 2555;

            tool.OpParams = opParams;

            // Set the changed Tool into the Assembly.
            assembly.MainTool = tool;

            // Add a segment to the default holder that was created
            // and set it into the assembly.
            var holder = assembly.MainHolder;
            holder.Name = "My Holder";
            holder.AddSegment(3, 3, 1); // Add more segments?

            // ** IMPORTANT ***
            // If the assembly was a default assembly before making this change, we need to set the "IsDefault" property
            // on the assembly to make Mastercam understand that this is now a user defined assembly.  Default assemblies
            // in Mastercam are treated as regular tools with a default holder.  Non-default assemblies are presented
            // differently in the tool lists throughout Mastercam through the filtering options and name outputs.
            // This step will not be required in the long-term, but for the short-term, setting this property is necessary
            var set = assembly.SetAsDefaultAssembly(false);
            Debug.Assert(set, "Assembly is still set as a default assembly");

            return assembly;
        }

        /// <summary> The main entry point for your CreateMillTool. </summary>
        ///
        /// <param name="param"> System parameter. </param>
        ///
        /// <returns> A <c>MCamReturn</c> return type representing the outcome of your NetHook application. </returns>

    }
}
