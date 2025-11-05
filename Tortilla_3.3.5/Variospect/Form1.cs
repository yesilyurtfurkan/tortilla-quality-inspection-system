using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using HalconDotNet;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

namespace Variospect
{
    public partial class Form1 : Form
    {

        HDevEngine hDevengine = new HDevEngine();
        HDevProcedureCall hDevFGOpenerProcCall;
        HDevProcedureCall hDevGrabProcCall;

        HDevProcedureCall hDevReadModels;
        HDevProcedureCall hDevTrainModel;
        HDevProcedureCall hDevDoControl;
        HDevProcedureCall hDevPrepareTile;
        HDevProcedureCall hDevTile;
        HDevProcedureCall hReadModel;

        HTuple ht_Model = new HTuple();
        HTuple DLPreprocessParam = new HTuple();
        HTuple DLModelHandle = new HTuple();
        HTuple DLDeviceHandles = new HTuple();
        HTuple DLDevice = new HTuple();
        HTuple ClassNames = new HTuple();
        HTuple ClassIDs = new HTuple();
        HTuple DLDataInfo = new HTuple();
        HTuple TresholdOfset = new HTuple();

        HTupleType HANDLE;
        HTuple hv_AcqHandle1 = null;
        HTuple Params1 = null;
        HImage ho_Image1 = new HImage();
        HImage ho_Image2 = new HImage();

        HObject[] ho_Image1D = new HObject[6];
        HTuple Classes;
        HTuple Train;
        HTuple MLPHandle;
        HTuple NumClasses;

        HTuple Result;
        HTuple M1;
        HTuple M2;
        HTuple M3;
        HTuple H1;
        HTuple H2;
        HTuple H3;
        HTuple W;
        HTuple Confidence;


        HTuple ImageWidth = 4096;
        HTuple ImageHeight = 128;
        HTuple MaxImagesRegions = 12;

        HImage TiledImageMinusOldest = new HImage();
        HImage ImagesToTile = new HImage();
        HImage TiledImage = new HImage();
        HImage PrevRegions = new HImage();

        HImage TiledImageMinusOldestOut = new HImage();
        HImage ImagesToTileOut = new HImage();
        HImage TiledImageOut = new HImage();

        List<double> p2Hlist = new List<double>();
        List<double> p2Llist = new List<double>();
        int logCounter = 0;

        string plcAddress = "01";
        const int controlCount = 18;
        bool systemStarted = false;
        HWindow[] HWindows1 = new HWindow[18];
        int readCounter = 0;
        bool plc_M100 = false;
        bool plc_M101 = false;
        //bool plc_M102 = false;
        int ASenkron = 0;
        string plc_MRegs = string.Empty;
        bool cam1Ready = false;
        int plcLocX = 0;
        double camLocX = 0;
        int SayıCark = 0;
        HObject[] ho_Images1 = new HObject[18];

        private System.Timers.Timer timer4;

        string sifre = "";
        string lg_s = "tr";

        bool SistemCalisma = true;
        bool AcilStop = false;
        bool DriverError = false;
        public Form1()
        {
            InitializeComponent();

            backgroundWorker1.WorkerReportsProgress = true; //backgroundWorker1 için işlemin ilerlemesini raporlama aktif edildi
            backgroundWorker1.WorkerSupportsCancellation = true; //backgroundWorker1 için işlemin durdurulabilme aktif edildi

            backgroundWorker2.WorkerReportsProgress = true; //backgroundWorker1 için işlemin ilerlemesini raporlama aktif edildi
            backgroundWorker2.WorkerSupportsCancellation = true; //backgroundWorker1 için işlemin durdurulabilme aktif edildi

            backgroundWorker3.WorkerReportsProgress = true; //backgroundWorker1 için işlemin ilerlemesini raporlama aktif edildi
            backgroundWorker3.WorkerSupportsCancellation = true; //backgroundWorker1 için işlemin durdurulabilme aktif edildi
        
            Control.CheckForIllegalCrossThreadCalls = false;
            timer4 = new System.Timers.Timer();
            timer4.Interval = 1; // Timer aralığı (ms) - Örneğin, her 1 saniyede bir
            timer4.Elapsed += TimerElapsed4;
        }

        private bool OpenSerialPort()
        {
            CloseSerialPort();
            if (!cbPort.Text.Equals(string.Empty))
            {
                spPLC.PortName = cbPort.Text;
                try
                {
                    spPLC.Open();
                    plc.BackColor = Color.ForestGreen;
                    StatusLog(DateTime.Now.ToString("HH:mm:ss.fff") + " PLC Bağlanıldı", Color.Green);
                    AddToLog("PLC", "PLC Bağlantısı Başarılı", 2, Color.Green);
                    //
                    //timer1.Enabled = true;
                    DplcOku(bnt_count_one, out string myStr);  // Modüler Bant Okuma
                    if (myStr.Length > 3)
                    {
                        string sonKarakter = myStr.Substring(0, 2);
                        txt_modular_spd.Text = sonKarakter;
                    }
                    DplcOku(bnt_count_two, out string myStr2);  // ayıklama Bant Okuma
                    if (myStr2.Length > 3)
                    {
                        string sonKarakter2 = myStr2.Substring(0, 2);
                        txt_sorting_spd.Text = sonKarakter2;
                    }
                    else
                    {
                        txt_sorting_spd.Text = "0";
                    }

                    //  timer2.Enabled = true;
                    timer4.Start();
                }
                catch (Exception ex)
                {
                    AddToLog("Seri Port", ex.Message, 3, Color.Red);
                    StatusLog(DateTime.Now.ToString("HH:mm:ss.fff") + " PLC Bağlanma Hatası", Color.Red);
                    plc.BackColor = Color.Red;
                    //timer1.Enabled = false;
                    //timer4.Stop();
                }
            }
            else AddToLog("Seri Port", "Port Belirtilmemiş. PLC ye bağlanmak için seri COM port seçiniz!", 6, Color.Red);

            //pbGreen.Visible = spPLC.IsOpen;
            //pbRed.Visible = !spPLC.IsOpen;

            return spPLC.IsOpen;
        }

        private void CloseSerialPort()
        {
            if (spPLC.IsOpen)
            {
                spPLC.Close();
                plc.BackColor = Color.Red;

            }


            //pbGreen.Visible = spPLC.IsOpen;
            //pbRed.Visible = !spPLC.IsOpen;
        }

        private bool InitEnv()
        {
            bool r = OpenSerialPort();
            if (r)
            {

            }
            return r;
        }

        private void LoadConfigs()
        {
            cbPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cbPort.Items.Add(port);
            }

            string cfgFile = System.Windows.Forms.Application.StartupPath + "\\Configs.dat";
            if (File.Exists(cfgFile))
            {
                string[] lines = File.ReadAllLines(cfgFile);
                try
                {
                    cbPort.Text = lines[0];
                    bnt_count_one.Value = Convert.ToInt32(lines[1]);
                    bnt_count_two.Value = Convert.ToInt32(lines[2]);
                    zone_shot_count_one.Value = Convert.ToInt32(lines[3]);
                    zone_shot_count_two.Value = Convert.ToInt32(lines[4]);
                    zone_shot_count_three.Value = Convert.ToInt32(lines[5]);
                    zone_size_count_one.Value = Convert.ToInt32(lines[6]);
                    zone_size_count_two.Value = Convert.ToInt32(lines[7]);
                    zone_size_count_three.Value = Convert.ToInt32(lines[8]);
                    zone_width_count_one.Value = Convert.ToInt32(lines[9]);
                    zone_width_count_two.Value = Convert.ToInt32(lines[10]);
                    zone_width_count_three.Value = Convert.ToInt32(lines[11]);
                    working_control_count.Value = Convert.ToInt32(lines[12]);
                    c1u.Checked = Convert.ToBoolean(lines[13]);
                    cbAuto.Checked = Convert.ToBoolean(lines[14]);
                    txt_folder_img.Text = lines[15];
                    //txt_min_en.Text = lines[16];
                    //txt_max_en.Text = lines[17];
                    //txt_min_boy.Text = lines[18];
                    //txt_max_boy.Text = lines[19];
                    numericUpDown1.Value = Convert.ToInt32(lines[20]);
                    numericUpDown2.Value = Convert.ToInt32(lines[21]);
                    numericUpDown3.Value = Convert.ToInt32(lines[22]);
                    numericUpDown4.Value = Convert.ToInt32(lines[23]);
                    numericUpDown6.Value = Convert.ToInt32(lines[24]);
                    numericUpDown5.Value = Convert.ToInt32(lines[25]);
                    AddToLog("Config", "Ayarlar Başarıyla Yüklendi! ", 2, Color.Green);
                    approval.BackColor = Color.ForestGreen;

                }
                catch (Exception ex)
                {
                    AddToLog("Config", "Ayarlar Yüklenemedi! " + ex.Message, 6, Color.Red);
                    approval.BackColor = Color.Red;
                }
            }
        }

        private void SaveSettings()
        {
            using (StreamWriter sw = new StreamWriter(System.Windows.Forms.Application.StartupPath + "\\Configs.dat"))
            {
                sw.WriteLine(cbPort.Text);
                sw.WriteLine(bnt_count_one.Value.ToString());
                sw.WriteLine(bnt_count_two.Value.ToString());
                sw.WriteLine(zone_shot_count_one.Value.ToString());
                sw.WriteLine(zone_shot_count_two.Value.ToString());
                sw.WriteLine(zone_shot_count_three.Value.ToString());
                sw.WriteLine(zone_size_count_one.Value.ToString());
                sw.WriteLine(zone_size_count_two.Value.ToString());
                sw.WriteLine(zone_size_count_three.Value.ToString());
                sw.WriteLine(zone_width_count_one.Value.ToString());
                sw.WriteLine(zone_width_count_two.Value.ToString());
                sw.WriteLine(zone_width_count_three.Value.ToString());
                sw.WriteLine(working_control_count.Value.ToString());
                sw.WriteLine(c1u.Checked.ToString());
                sw.WriteLine(cbAuto.Checked.ToString());
                sw.WriteLine(txt_folder_img.Text);
                sw.WriteLine("1");
                sw.WriteLine("1");
                sw.WriteLine("1");
                sw.WriteLine("1");
                sw.WriteLine(numericUpDown1.Value.ToString());
                sw.WriteLine(numericUpDown2.Value.ToString());
                sw.WriteLine(numericUpDown3.Value.ToString());
                sw.WriteLine(numericUpDown4.Value.ToString());
                sw.WriteLine(numericUpDown6.Value.ToString());
                sw.WriteLine(numericUpDown5.Value.ToString());
            }
            MessageBox.Show("Ayarlar Başarı ile Kayıt Edildi");
            AddToLog("Kaydetme", "Ayarlar Başarıyla Kaydedildi.", 10, Color.Green);
        }

        private bool InitHALCON(out string msg)
        {
            bool retVal = false;
            msg = "";
            hDevengine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            HOperatorSet.SetSystem("parallelize_operators", "true");
             string devFile = System.Windows.Forms.Application.StartupPath + "\\Line.hDev";
          //  string devFile = System.Windows.Forms.Application.StartupPath + "\\LineSim.hDev";

            if (File.Exists(devFile))
            {
                try
                {
                    hDevengine.SetProcedurePath(System.Windows.Forms.Application.StartupPath);

                    HDevProgram hDevProgram = new HDevProgram(devFile);

                    HDevProcedure hDevOpenerProc = new HDevProcedure(hDevProgram, "OpenGrabber1");
                    HDevProcedure hDevGrabberProc = new HDevProcedure(hDevProgram, "GrabImage");
                    HDevProcedure hDevReadModelsProc = new HDevProcedure(hDevProgram, "ReadModels");
                    HDevProcedure hDevTrainModelProc = new HDevProcedure(hDevProgram, "TrainModel");
                    HDevProcedure hDevDoControlProc = new HDevProcedure(hDevProgram, "DoControl");
                    HDevProcedure hDevPrepareProc = new HDevProcedure(hDevProgram, "PrepareTile");
                    HDevProcedure hDevTileProc = new HDevProcedure(hDevProgram, "Tile");
                    HDevProcedure hDevProcReadModel = new HDevProcedure(hDevProgram, "ReadModel");
                    hDevFGOpenerProcCall = new HDevProcedureCall(hDevOpenerProc);
                    hDevGrabProcCall = new HDevProcedureCall(hDevGrabberProc);
                    hDevReadModels = new HDevProcedureCall(hDevReadModelsProc);
                    hDevTrainModel = new HDevProcedureCall(hDevTrainModelProc);
                    hDevDoControl = new HDevProcedureCall(hDevDoControlProc);
                    hDevPrepareTile = new HDevProcedureCall(hDevPrepareProc);
                    hReadModel = new HDevProcedureCall(hDevProcReadModel);
                    hDevTile = new HDevProcedureCall(hDevTileProc);

                    hDevGrabProcCall.SetInputCtrlParamTuple("WinHandle1", hWin1.HalconWindow);
                    //hDevGrabProcCall.SetInputCtrlParamTuple("WinHandle2", hWin2.HalconWindow);

                    retVal = true;
                    msg = devFile + " Loaded.";

                    hWin1.HalconWindow.SetDraw("margin");
                    hWin1.HalconWindow.SetColored(12);
                    info.BackColor = Color.ForestGreen;

                    //hWin2.HalconWindow.SetDraw("margin");
                    //hWin2.HalconWindow.SetColored(12);
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    info.BackColor = Color.Red;
                }
            }
            else
            {
                msg = "Image processing script file (" + devFile + ") not found!";
                info.BackColor = Color.Red;
                //tbMain.Enabled = false;
            }

            AddToLog("Kütüphane", msg, retVal ? 1 : 6, retVal ? Color.Green : Color.Red);

            return retVal;

        }
        private bool ReadModel()
        {
            bool Model = false;
            ht_Model = 1;
            try
            {
                hReadModel.SetInputCtrlParamTuple("Model", ht_Model);
                hReadModel.Execute();
                DLPreprocessParam = hReadModel.GetOutputCtrlParamTuple("DLPreprocessParam");
                DLModelHandle = hReadModel.GetOutputCtrlParamTuple("DLModelHandle");
                DLDeviceHandles = hReadModel.GetOutputCtrlParamTuple("DLDeviceHandles");
                DLDevice = hReadModel.GetOutputCtrlParamTuple("DLDevice");
                ClassNames = hReadModel.GetOutputCtrlParamTuple("ClassNames");
                ClassIDs = hReadModel.GetOutputCtrlParamTuple("ClassIDs");
                DLDataInfo = hReadModel.GetOutputCtrlParamTuple("DLDataInfo");

                AddToLog("Image Processing", "DlHandle Okundu", 0, Color.Gray);
        
            }
            catch (Exception ex)
            {
                AddToLog("Vision Err", ex.Message, 5, Color.Red);
            }

            return Model;
        }

        private void StatusLog(string msg, Color clr)
        {
            lbMsg.Text = msg;
            lbMsg.ForeColor = clr;
        }

        private void StatusLog2(string msg, Color clr)
        {
            lPulse.Text = msg;
            lPulse.ForeColor = clr;
        }

        private void AddToLog(string grp, string dtl, int imgidx, Color clr)
        {
            if (logCounter >= 1000) logCounter = 0;

            StatusLog(grp + " - " + dtl, clr);

            int maxCap = 36;
            if (listView1.Items.Count > maxCap) listView1.Items.Clear();

            ListViewItem listItem = new ListViewItem(logCounter.ToString());
            listItem.ImageIndex = imgidx;
            listItem.SubItems.Add(DateTime.Now.ToString("HH:mm:ss.fff"));
            listItem.SubItems.Add(grp);
            listItem.SubItems.Add(dtl);
            listView1.Items.Add(listItem);

            listView1.Items[listView1.Items.Count - 1].ForeColor = clr;
            listView1.Refresh();
            logCounter++;
            if (logCounter > 999) logCounter = 0;
        }

        private bool OpenCamera()
        {
            bool retVal = false;
            int cCount = 0;

            string folder1 = @"C:\Dosyalar\ProjectImages\Tortilla";

            try
            {
                string Cam="File";
                if(c1u.Checked)
                {
                    Cam = "GigE";
                }
                hDevFGOpenerProcCall.SetInputCtrlParamTuple("Interface1", Cam);
                hDevFGOpenerProcCall.SetInputCtrlParamTuple("Source1", folder1);
          
                hDevFGOpenerProcCall.SetInputCtrlParamTuple("WinHandle1", hWin1.HalconWindow);
                hDevFGOpenerProcCall.SetInputCtrlParamTuple("ImageHeight", ImageHeight);
                hDevFGOpenerProcCall.Execute();

                hv_AcqHandle1 = hDevFGOpenerProcCall.GetOutputCtrlParamTuple("AcqHandle_1");
                Params1 = hDevFGOpenerProcCall.GetOutputCtrlParamTuple("Params1");

            }
            catch (Exception ex)
            {
                //tbMain.Enabled = bStart.Enabled = bGrab.Enabled = bProcess.Enabled = false;
                AddToLog("Kamera Hatası", ex.Message, 9, Color.Red);
            }

            retVal = true;
            return retVal;
        }

        private bool GrabImage(bool asynch)
        {
            bool retVal = false;
            int async = asynch ? 1 : 0;

            if (!asynch)
            {
                //if (tbLive.Checked)
                //{
                //    tbLive.Checked = false;
                //    tmrLive.Enabled = false;
                //}
            }

            try
            {
                hDevGrabProcCall.SetInputCtrlParamTuple("AcqHandle_1", hv_AcqHandle1);
                hDevGrabProcCall.SetInputCtrlParamTuple("WinHandle1", hWin1.HalconWindow);


                hDevGrabProcCall.Execute();

                retVal = hDevGrabProcCall.GetOutputCtrlParamTuple("GrabResult").I == 1;

                // hWin1.SetFullImagePart(null);
                ho_Image1 = hDevGrabProcCall.GetOutputIconicParamImage("Image");
            }
            catch(Exception  ex) { }

            //if ((cbAuto.Checked) && (!tbLive.Checked))
            //    SaveCurImages();

            return retVal;
        }

        private bool ReadModels(bool asynch)
        {
            bool retVal = true;
            try
            {
                hDevReadModels.Execute();

                Classes = hDevReadModels.GetOutputCtrlParamTuple("Classes");
                MLPHandle = hDevReadModels.GetOutputCtrlParamTuple("MLPHandle");
                NumClasses = hDevReadModels.GetOutputCtrlParamTuple("NumClasses");

            }
            catch (Exception ex)
            {
                AddToLog("ReadModels", "Yüklenemedi " + ex.Message, 6, Color.Red);

            }
            return retVal;
        }

        private bool PrepareaTile(bool asynch)
        {
            bool retVal = true;
            try
            {
                hDevPrepareTile.SetInputCtrlParamTuple("MaxImagesRegions", MaxImagesRegions);
                hDevPrepareTile.SetInputCtrlParamTuple("ImageWidth", ImageWidth);
                hDevPrepareTile.SetInputCtrlParamTuple("ImageHeight", ImageHeight);
                hDevPrepareTile.Execute();

                TiledImageMinusOldest = hDevPrepareTile.GetOutputIconicParamImage("TiledImageMinusOldest");
                ImagesToTile = hDevPrepareTile.GetOutputIconicParamImage("ImagesToTile");
                TiledImage = hDevPrepareTile.GetOutputIconicParamImage("TiledImage");
                //  PrevRegions = hDevPrepareTile.GetOutputIconicParamImage("PrevRegions");

            }
            catch (Exception ex)
            {
                AddToLog("PrepareTile", "Hata " + ex.Message, 6, Color.Red);

            }

            return retVal;
        }

        private bool Tile(bool asynch)
        {
            bool retVal = true;
            try
            {
                hDevTile.SetInputIconicParamObject("TiledImage", TiledImage);
                hDevTile.SetInputIconicParamObject("TiledImageMinusOldest", TiledImageMinusOldest);
                hDevTile.SetInputIconicParamObject("Image", ho_Image1);
                hDevTile.SetInputIconicParamObject("ImagesToTile", ImagesToTile);

                hDevTile.SetInputCtrlParamTuple("MaxImagesRegions", MaxImagesRegions);
                hDevTile.SetInputCtrlParamTuple("ImageWidth", ImageWidth);
                hDevTile.SetInputCtrlParamTuple("ImageHeight", ImageHeight);
                hDevTile.SetInputCtrlParamTuple("WinHandle1", hWin1.HalconWindow);

                hDevTile.Execute();

                TiledImageMinusOldestOut = hDevTile.GetOutputIconicParamImage("TiledImageMinusOldestOut");
                ImagesToTileOut = hDevTile.GetOutputIconicParamImage("ImagesToTileOut");
                TiledImage = hDevTile.GetOutputIconicParamImage("TiledImageOut");

            }
            catch (Exception ex)
            {
                AddToLog("Tile ", "Hata " + ex.Message, 6, Color.Red);

            }
            return retVal;
        }

        private bool TrainModels(bool asynch)
        {
            bool retVal = true;
            try
            {
                hDevTrainModel.Execute();

                Train = hDevTrainModel.GetOutputCtrlParamTuple("Train");
                Classes = hDevTrainModel.GetOutputCtrlParamTuple("Classes");

            }
            catch (Exception ex)
            {
                AddToLog("Train Models", "Train Model Hata" + ex.Message, 6, Color.Red);
                approval.BackColor = Color.Red;
            }
            return retVal;
        }

        private void DoControl()
        {
            HImage ho_Image11 = new HImage();
            try
            {
                int Seviye1 = 0;
                int Seviye2 = 0;
                int Seviye3 = 0;
                int Seviye = 0;

                if (label6.BackColor == Color.Red) // Çok pişmiş
                {
                    Seviye3 = 1;
                }
                if (label7.BackColor == Color.Red) // oRTA pişmiş
                {
                    Seviye2 = 1;
                }
                if (label10.BackColor == Color.Red) // az pişmiş
                {
                    Seviye1 = 1;
                }
                if (label9.BackColor == Color.Red) // pişmemiş
                {
                    Seviye = 1;
                } 
                //hDevDoControl.SetInputCtrlParamTuple("MLPHandle", MLPHandle);
                hDevDoControl.SetInputCtrlParamTuple("WinHandle1", hWin1.HalconWindow);
                hDevDoControl.SetInputIconicParamObject("Image", TiledImage);
                hDevDoControl.SetInputCtrlParamTuple("DLPreprocessParam", DLPreprocessParam);
                hDevDoControl.SetInputCtrlParamTuple("DLModelHandle", DLModelHandle);
                hDevDoControl.SetInputCtrlParamTuple("Hmincm", Convert.ToInt32(numericUpDown3.Value));
                hDevDoControl.SetInputCtrlParamTuple("Hmaxcm", Convert.ToInt32(numericUpDown4.Value));
                hDevDoControl.SetInputCtrlParamTuple("Threshold", Convert.ToInt32(numericUpDown6.Value));
                hDevDoControl.SetInputCtrlParamTuple("Katlanma", Convert.ToInt32(numericUpDown5.Value));

                hDevDoControl.SetInputCtrlParamTuple("Seviye3", Seviye3);
                hDevDoControl.SetInputCtrlParamTuple("Seviye2", Seviye2);
                hDevDoControl.SetInputCtrlParamTuple("Seviye1", Seviye1);
                hDevDoControl.SetInputCtrlParamTuple("Seviye", Seviye);

                hDevDoControl.Execute();
            }
            catch (Exception ex)
            {
                AddToLog("DoControl", "Hata " + ex.Message, 6, Color.Red);
            }
                 ho_Image11 = hDevDoControl.GetOutputIconicParamImage("Images");
                //Result = hDevDoControl.GetOutputCtrlParamTuple("Result");
                //HTuple  ElemanSay = hDevDoControl.GetOutputCtrlParamTuple("Eleman");

                M1 = hDevDoControl.GetOutputCtrlParamTuple("M1");
                M2 = hDevDoControl.GetOutputCtrlParamTuple("M2");
                M3 = hDevDoControl.GetOutputCtrlParamTuple("M3");
               // W = hDevDoControl.GetOutputCtrlParamTuple("W");
                H1 = hDevDoControl.GetOutputCtrlParamTuple("H1");
                H2 = hDevDoControl.GetOutputCtrlParamTuple("H2");
                H3 = hDevDoControl.GetOutputCtrlParamTuple("H3");

                Confidence = hDevDoControl.GetOutputCtrlParamTuple("Confidence");
              
                //int CountImages = ho_Image11.CountObj();
                // string sonuc = Result.ToString();

                //lblPos2.Text = sonuc.ToString();



            if (plc.BackColor == Color.ForestGreen)
                {
                //AddToLog("Sistem", "Bilgisayar hafızasının", 7, Color.Orange);
                if (M1.ToString() == "1")
                    {
                    if (backgroundWorker1.IsBusy != true)
                    {
                        backgroundWorker1.RunWorkerAsync();
                    }
                    }     

                    if (M2.ToString() == "1")
                    {
                    if (backgroundWorker2.IsBusy != true)
                    {
                        backgroundWorker2.RunWorkerAsync();
                    }

                    }

                    if (M3.ToString() == "1")
                    {
                    if (backgroundWorker3.IsBusy != true)
                    {
                        backgroundWorker3.RunWorkerAsync();
                    }

                    }
                }



            //if (CountImages > 0)
            //{
            //    for (int i = 0; i < CountImages; i++)
            //    {
            //        ASenkron++;
            //        if (ASenkron <= controlCount)
            //        {
            //            HWindows1[ASenkron - 1].ClearWindow();

            //            HOperatorSet.SelectObj(ho_Image11, out HObject hdd, (i + 1));
            //            HWindows1[ASenkron - 1].DispObj(hdd);
            //            // HOperatorSet.CopyObj(ho_Image1D[0], out ho_Images1[ASenkron - 1], 1, -1);

            //          
            //        }
            //        else
            //        {
            //            ASenkron = 0;
            //        }
            //    }
            //}


        }
        private void Form1_Load(object sender, EventArgs e)
        {
            long yuzde = GetDriveStatus("C:\\");
            AddToLog("Sistem", "Bilgisayar hafızasının " + Convert.ToString(yuzde) + " %  sı dolu. Lüffen verilerini yedekleyiniz", 7, Color.Orange);
            if (Convert.ToInt32(yuzde) > 90)
            {
                MessageBox.Show("Bilgisayar Hafızası Dolu Lütfen Verileri Yedekleyiniz");
            }
    

            StatusLog2("Sistem Çalışmaya Hazır", Color.Green);
        }

        long GetDriveStatus(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName) { return ((drive.TotalSize - drive.TotalFreeSpace) * 100) / drive.TotalSize; }
            }

            return -1;
        }

        private void tmrAO_Tick(object sender, EventArgs e)
        {
            tmrAO.Enabled = false;
           
            bool h = InitHALCON(out string msg);
            if (h)
            {
                ReadModels(true);
                ReadModel();
                h = OpenCamera();
                PrepareaTile(true);
                if (h)
                {
                    InitEnv();
                    //GrabImage(true);
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
        
            //HWindows1[0] = hw1_0.HalconWindow;
            //HWindows1[1] = hw2_0.HalconWindow;
            //HWindows1[2] = hw3_0.HalconWindow;
            //HWindows1[3] = hw4_0.HalconWindow;
            //HWindows1[4] = hw5_0.HalconWindow;
            //HWindows1[5] = hw6_0.HalconWindow;
            //HWindows1[6] = hw7_0.HalconWindow;
            //HWindows1[7] = hw8_0.HalconWindow;
            //HWindows1[8] = hw9_0.HalconWindow;
            //HWindows1[9] = hw10_0.HalconWindow;
            //HWindows1[10] = hw11_0.HalconWindow;
            //HWindows1[11] = hw12_0.HalconWindow;
            //HWindows1[12] = hw13_0.HalconWindow;
            //HWindows1[13] = hw14_0.HalconWindow;
            //HWindows1[14] = hw15_0.HalconWindow;
            //HWindows1[15] = hw16_0.HalconWindow;
            //HWindows1[16] = hw17_0.HalconWindow;
            //HWindows1[17] = hw18_0.HalconWindow;

            //  GrabImage(true);
            //btLogin.BackColor = Color.Red;

            for (int i = 0; i < controlCount; i++)
            {
                ho_Images1[i] = new HObject();

                //hOset_display_font(HWindows1[i], 16, "mono", "true", "false");
                //set_display_font(HWindows2[i], 16, "mono", "true", "false");
            }

            LoadConfigs();
            tmrAO.Enabled = true;
        }
        #region MODBUS


        private string Hex2(int x)
        {
            string s = x.ToString("X2");
            //if (s.Length < 2) s = "0" + s;
            return s;
        }

        private string Hex4(int x)
        {
            string s = x.ToString("X4");
            //while (s.Length < 4) s = "0" + s;
            return s;
        }

        private void GiveMessage(string msg, bool retVal)
        {
            AddToLog("ModBUS", msg, retVal ? 10 : 6, retVal ? Color.Black : Color.Red);
        }

        private static string checkSum(string writeUncheck)
        {
            char[] hexArray = new char[writeUncheck.Length];
            hexArray = writeUncheck.ToCharArray();
            int decNum = 0, decNumMSB = 0, decNumLSB = 0;
            int decByte, decByteTotal = 0;

            bool msb = true;

            for (int t = 0; t <= hexArray.GetUpperBound(0); t++)
            {
                if ((hexArray[t] >= 48) && (hexArray[t] <= 57))

                    decNum = (hexArray[t] - 48);

                else if ((hexArray[t] >= 65) & (hexArray[t] <= 70))
                    decNum = 10 + (hexArray[t] - 65);

                if (msb)
                {
                    decNumMSB = decNum * 16;
                    msb = false;
                }
                else
                {
                    decNumLSB = decNum;
                    msb = true;
                }
                if (msb)
                {
                    decByte = decNumMSB + decNumLSB;
                    decByteTotal += decByte;
                }
            }

            decByteTotal = (255 - decByteTotal) + 1;
            decByteTotal = decByteTotal & 255;

            int a, b = 0;

            string hexByte = "", hexTotal = "";
            double i;

            for (i = 0; decByteTotal > 0; i++)
            {
                b = Convert.ToInt32(System.Math.Pow(16.0, i));
                a = decByteTotal % 16;
                decByteTotal /= 16;
                if (a <= 9)
                    hexByte = a.ToString();
                else
                {
                    switch (a)
                    {
                        case 10:
                            hexByte = "A";
                            break;
                        case 11:
                            hexByte = "B";
                            break;
                        case 12:
                            hexByte = "C";
                            break;
                        case 13:
                            hexByte = "D";
                            break;
                        case 14:
                            hexByte = "E";
                            break;
                        case 15:
                            hexByte = "F";
                            break;
                    }
                }
                hexTotal = String.Concat(hexByte, hexTotal);
            }

            while (hexTotal.Length < 2)
                hexTotal = "0" + hexTotal;

            return hexTotal;

        }

        private bool SendToCom(string write, out string send, out string response)
        {
            bool retVal = false;
            string readBuffer = "";
            string sendWord = write;

            string hexTotal = checkSum(sendWord);
            send = string.Concat(":", sendWord, hexTotal);

            string writeBuffer = string.Concat(send, "\r");

            try
            {
                spPLC.DiscardInBuffer();
                spPLC.DiscardOutBuffer();
                spPLC.ReadExisting();

                spPLC.WriteLine(writeBuffer);
                readBuffer = spPLC.ReadLine();
                retVal = true;
            }
            catch (System.Exception ex)
            {
                readBuffer = ex.Message;
            }
            finally
            {
            }
            response = readBuffer;
            if (retVal)
                retVal = response.StartsWith(send.Substring(0, 5));
            return retVal;
        }

        private bool Set_Single_M_Register(byte mNo, out string send, out string response)
        {
            response = "";
            send = "";
            bool retVal = false;
            if (spPLC.IsOpen)
            {
                string register = "08" + Hex2(mNo);
                string data = "FF00";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Set Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Set_Single_Y_Register(byte mNo, out string send, out string response)
        {
            response = "";
            send = "";
            bool retVal = false;
            if (spPLC.IsOpen)
            {
                string register = "05" + Hex2(mNo);
                string data = "FF00";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. Y" + mNo.ToString() + " Set Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Set_Single_M_Register_Hi(int mNo, out string send, out string response)
        {
            response = "";
            send = "";
            bool retVal = false;
            if (spPLC.IsOpen)
            {
                string regAddr = "08";
                if (mNo > 255)
                {
                    regAddr = "09";
                    mNo -= 256;
                }
                string register = regAddr + Hex2(mNo);
                string data = "FF00";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Set Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Force_M_Register_On(byte mNo, out string send, out string response)
        {
            response = "";
            send = "";
            bool retVal = Set_Single_M_Register(mNo, out send, out response);
            if (!retVal)
                retVal = Set_Single_M_Register(mNo, out send, out response);
            if (retVal)
            {
                string git, gel;
                bool val;
                bool bRead = Read_Single_M_Register(mNo, out git, out gel, out val);
                retVal = val;
            }
            return retVal;
        }

        private bool Clear_Single_M_Register(byte mNo, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                string register = "08" + Hex2(mNo);
                string data = "0000";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Clear Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Clear_Single_Y_Register(byte mNo, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                string register = "05" + Hex2(mNo);
                string data = "0000";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. Y" + mNo.ToString() + " Clear Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Clear_Single_M_Register_Hi(int mNo, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                string regAddr = "08";
                if (mNo > 255)
                {
                    regAddr = "09";
                    mNo -= 256;
                }

                string register = regAddr + Hex2(mNo);
                string data = "0000";

                string tx = string.Concat(plcAddress, "05", register, data);
                retVal = SendToCom(tx, out send, out response);
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Clear Edilemez!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Write_D_Register(byte dNo, string dVal, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                //string plcAddress = "00";
                string register = "10" + Hex2(dNo);
                string data = dVal;

                string tx = string.Concat(plcAddress, "06", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                    retVal = response.StartsWith(send.Substring(0, 6));
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. D" + dNo.ToString() + " Yazılamadı!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Write_D_Register_16(byte dNo, int dVal, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                //string plcAddress = "00";
                string register = "10" + Hex2(dNo);
                string data = Hex4(dVal);

                string tx = string.Concat(plcAddress, "06", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                    retVal = response.StartsWith(send.Substring(0, 6));
                if (!retVal)
                {
                    txSend.Text = send;
                    txResp.Text = response;
                    AddToLog("PLC Yazma Hatası", "D" + dNo.ToString() + " Yazılamadı. Giden:" + send + " Gelen:" + response, 12, Color.Red);
                }
            }
            else
            {
                response = spPLC.PortName + " Açık Değil. D" + dNo.ToString() + " Yazılamadı!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Write_D_Register_32(byte dNo, int dVal, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                //string plcAddress = "00";
                string register = "10" + Hex2(dNo);

                string dWord = dVal.ToString("X8");
                string loWord = dWord.Substring(4, 4);
                string hiWord = dWord.Substring(0, 4);

                string tx = string.Concat(plcAddress, "10", register, "0002", "04", loWord, hiWord);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                    retVal = response.StartsWith(send.Substring(0, 6));

            }
            else
            {
                response = spPLC.PortName + " Açık Değil. D" + dNo.ToString() + " Yazılamadı!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private bool Write_C_Register_32(byte dNo, int dVal, out string send, out string response)
        {
            bool retVal = false;
            response = "";
            send = "";
            if (spPLC.IsOpen)
            {
                //string plcAddress = "00";
                string register = "0E" + Hex2(dNo);

                string dWord = dVal.ToString("X8");
                string loWord = dWord.Substring(4, 4);
                string hiWord = dWord.Substring(0, 4);

                string tx = string.Concat(plcAddress, "10", register, "0002", "04", loWord, hiWord);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                    retVal = response.StartsWith(send.Substring(0, 6));

            }
            else
            {
                response = spPLC.PortName + " Açık Değil. C" + dNo.ToString() + " Yazılamadı!";
                GiveMessage(response, false);
            }
            return retVal;
        }

        private static bool TekSayi(int value)
        {
            return value % 2 != 0;
        }

        private string IntToBin8(int i)
        {
            string s = Convert.ToString(i, 2);
            while (s.Length < 8) s = "0" + s;
            return s;
        }

        private string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private string Bin2Bytes(int i1, int i2)
        {
            string s1 = ReverseString(IntToBin8(i1));
            string s2 = ReverseString(IntToBin8(i2));
            return s1 + s2;
        }

        private bool Read_Multi_M_Register(byte mNo, out string send, out string response, out string binValue)
        {
            bool retVal = false;
            binValue = string.Empty;
            send = "";
            response = "";
            if (spPLC.IsOpen)
            {
                string register = "08" + Hex2(mNo);
                string data = "000D"; // Data Count

                string tx = string.Concat(plcAddress, "01", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                {
                    //:11010101EC

                    if (response.StartsWith(":" + plcAddress + "0102"))
                    {
                        string data1 = response.Substring(7, 2);
                        string data2 = response.Substring(9, 2);
                        int iValue1 = Convert.ToInt16(data1, 16);
                        int iValue2 = Convert.ToInt16(data2, 16);
                        binValue = Bin2Bytes(iValue1, iValue2);
                    }
                    else retVal = false;
                }
            }
            else
            {
                GiveMessage(spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Okunamaz!", false);
            }
            return retVal;
        }

        private bool Read_Single_M_Register(byte mNo, out string send, out string response, out bool regValue)
        {
            bool retVal = false;
            regValue = false;
            send = "";
            response = "";
            if (spPLC.IsOpen)
            {
                string register = "08" + Hex2(mNo);
                string data = "0001";

                string tx = string.Concat(plcAddress, "01", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                {
                    //:11010101EC

                    if (response.StartsWith(":" + plcAddress + "0101"))
                    {
                        string pureData = response.Substring(7, 2);
                        int mValue = Convert.ToInt16(pureData, 16);
                        regValue = TekSayi(mValue);
                    }
                    else retVal = false;
                }
            }
            else
            {
                GiveMessage(spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Okunamaz!", false);
            }
            return retVal;
        }

        private bool Read_Single_X_Register(byte mNo, out string send, out string response, out bool regValue)
        {
            bool retVal = false;
            regValue = false;
            send = "";
            response = "";
            if (spPLC.IsOpen)
            {
                string register = "04" + Hex2(mNo);
                string data = "0001";

                string tx = string.Concat(plcAddress, "02", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                {
                    //:11010101EC

                    if (response.StartsWith(":" + plcAddress + "0201"))
                    {
                        string pureData = response.Substring(7, 2);
                        int mValue = Convert.ToInt16(pureData, 16);
                        regValue = TekSayi(mValue);
                    }
                    else retVal = false;
                }
            }
            else
            {
                GiveMessage(spPLC.PortName + " Açık Değil. M" + mNo.ToString() + " Okunamaz!", false);
            }
            return retVal;
        }

        private bool Read_Single_D_Register(byte mNo, out string send, out string response, out int regValue)
        {
            bool retVal = false;
            regValue = 0;
            send = "";
            response = "";
            if (spPLC.IsOpen)
            {
                string register = "10" + Hex2(mNo);
                string data = "0001";

                string tx = string.Concat(plcAddress, "03", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                {
                    //:11010101EC

                    if (response.StartsWith(":" + plcAddress + "0302"))
                    {
                        string pureData = response.Substring(7, 4);
                        regValue = Convert.ToInt16(pureData, 16);
                    }
                    else retVal = false;
                }
            }
            else
            {
                GiveMessage(spPLC.PortName + " Açık Değil. D" + mNo.ToString() + " Okunamaz!", false);
                AddToLog("PLC Okuma Hatası", spPLC.PortName + " Açık Değil. D" + mNo.ToString() + " Okunamaz!", 12, Color.Red);
            }
            return retVal;
        }

        private bool Read_Single_C_Register(byte mNo, out string send, out string response, out int regValue)
        {
            bool retVal = false;
            regValue = 0;
            send = "";
            response = "";
            if (spPLC.IsOpen)
            {
                string register = "0E" + Hex2(mNo);
                string data = "0001";

                string tx = string.Concat(plcAddress, "03", register, data);
                retVal = SendToCom(tx, out send, out response);
                if (retVal)
                {
                    //:11010101EC

                    if (response.StartsWith(":" + plcAddress + "0302"))
                    {
                        string pureData = response.Substring(7, 4);
                        regValue = Convert.ToInt16(pureData, 16);
                    }
                    else retVal = false;
                }
            }
            else
            {
                AddToLog("PLC Okuma Hatası", spPLC.PortName + " Açık Değil. C" + mNo.ToString() + " Okunamaz!", 12, Color.Red);
            }
            return retVal;
        }


        #endregion

        private void button6_Click(object sender, EventArgs e)
        {
            string giden, cevap;
            byte bNo = (byte)cbCount_one.Value;

            bool b = Set_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
            if (b) button8_Click(sender, e);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            bool b;
            int d;
            string rName = "M";
            string giden, gelen;
            string val;
            byte bNo = (byte)cbCount_one.Value;
            bool rOK = false;
            lbmerr.Text = "";
            if (cbRegister.SelectedIndex == 0)
            {
                rOK = Read_Single_M_Register(bNo, out giden, out gelen, out b);
                val = b.ToString();
            }
            else if (cbRegister.SelectedIndex == 1)
            {
                rOK = Read_Single_X_Register(bNo, out giden, out gelen, out b);
                rName = "X";
                val = b.ToString();
            }
            else
            {
                rOK = Read_Single_D_Register(bNo, out giden, out gelen, out d);
                rName = "D";
                val = d.ToString();
            }

            txResp.Text = gelen;
            txSend.Text = giden;

            if (rOK)
                GiveMessage(rName + bNo.ToString() + "=" + val, rOK);
            else
            {
                txResp.ForeColor = rOK ? Color.Green : Color.Red;
                if (!rOK)
                {
                    if (gelen.Length > 6)
                    {
                        string errCode = gelen.Substring(5, 2);
                        string errDesc = errCode;
                        if (errCode.Equals("01")) errDesc = "Illegal Command Code";
                        else if (errCode.Equals("02")) errDesc = "Illegal Device address";
                        else if (errCode.Equals("03")) errDesc = "Illegal Device Value";
                        else if (errCode.Equals("07")) errDesc = "CheckSum Error, Illegal command messages";
                        lbmerr.Visible = true;
                        lbmerr.Text = errDesc;
                    }
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string giden, cevap;
            byte bNo = (byte)cbCount_one.Value;
            bool b = Clear_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
            if (b) button8_Click(sender, e);
        }

        private void SetDPLC(NumericUpDown n1, string val)
        {
            byte bNo = (byte)n1.Value;
            int d = Convert.ToInt32(val);
            string giden, cevap;
            txSend.Text = "";
            txResp.Text = "";
            lbmerr.Text = "";
            lbmerr.Visible = false;
            bool b = false;

            b = Write_D_Register_16(bNo, d, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            byte bNo = (byte)cbCount_two.Value;
            int d = Convert.ToInt32(txdVal.Text);
            string giden, cevap;
            txSend.Text = "";
            txResp.Text = "";
            lbmerr.Text = "";
            lbmerr.Visible = false;
            bool b = false;
            if (cbDC.Text.StartsWith("D"))
            {
                if (rbStr.Checked)
                    b = Write_D_Register(bNo, txdVal.Text, out giden, out cevap);
                else
                {
                    if (rb32.Checked)
                        b = Write_D_Register_32(bNo, d, out giden, out cevap);
                    else
                        b = Write_D_Register_16(bNo, d, out giden, out cevap);
                }
            }
            else { b = Write_C_Register_32(bNo, d, out giden, out cevap); }

            txResp.Text = cevap;
            txSend.Text = giden;
            txResp.ForeColor = b ? Color.Green : Color.Red;
            if (!b)
            {
                if (cevap.Length > 6)
                {
                    string errCode = cevap.Substring(5, 2);
                    string errDesc = errCode;
                    if (errCode.Equals("01")) errDesc = "Illegal Command Code";
                    else if (errCode.Equals("02")) errDesc = "Illegal Device address";
                    else if (errCode.Equals("03")) errDesc = "Illegal Device Value";
                    else if (errCode.Equals("07")) errDesc = "CheckSum Error, Illegal command messages";
                    lbmerr.Visible = true;
                    lbmerr.Text = errDesc;
                }
            }
        }

        private void hWin1_MouseEnter(object sender, EventArgs e)
        {
            this.MouseWheel += hWin1.HSmartWindowControl_MouseWheel;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            OpenSerialPort();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            CloseSerialPort();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void ProcessPLCData(string regs)
        {
            if (regs.Length > 2)
            {
                if (!regs.Equals(plc_MRegs))
                {
                    plc_MRegs = regs;
                    bool bplc_M100 = regs[0].Equals("1"); // Take Photo
                    bool bplc_M101 = regs[1].Equals("1"); // Fine Tune
                    bool bplc_M102 = regs[2].Equals("1");  // Mac Ready

                    if (bplc_M102)
                    {

                        if (bplc_M100 != plc_M100)
                        {
                            plc_M100 = bplc_M100;
                            if ((plc_M100))
                            {
                                AddToLog("Sinyal", "Fotoğraf Sinyali Alındı.", 3, Color.Navy);

                            }
                        }

                        if (bplc_M101 != plc_M101)
                        {
                            plc_M101 = bplc_M101;
                            if ((plc_M101)) { AddToLog("Sinyal", "Hassas Ayar için Fotoğraf Sinyali Alındı.", 3, Color.Navy); }
                        }
                    }
                }
            }
        }

        private void ReadPLC()
        {
            string git, gel, regs;
            bool r = Read_Multi_M_Register(0, out git, out gel, out regs);
            if (r)
            {
                //if (!pbGreen.Visible) pbGreen.Visible = true;
                //if (pbRed.Visible) pbRed.Visible = false;
                ProcessPLCData(regs);

            }
            else
            {
                //if (pbGreen.Visible) pbGreen.Visible = false;
                //if (!pbRed.Visible) pbRed.Visible = true;
                StatusLog(DateTime.Now.ToString("HH:mm:ss.fff") + " PLC Okunamadı! (" + gel + ")", Color.Red);
            }
        }

        private bool StartSystem()
        {
            bool b = OpenSerialPort();
            if (b)
            {
                if (cam1Ready)
                    Set_Single_M_Register(107, out string git, out string gel);
                else
                    Clear_Single_M_Register(107, out string git, out string gel);
            }
            systemStarted = b;
            return b;
        }

        private void StopSystem()
        {
            CloseSerialPort();
            systemStarted = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void button16_Click(object sender, EventArgs e)
        {
            ReadPLC();
        }

        private void toolStripButton1_Click_2(object sender, EventArgs e)
        {

        }

        private void SaveCurImages(HObject h1Save, int CiMage)
        {

            string Dir = System.Windows.Forms.Application.StartupPath + "\\Images2\\" + DateTime.Now.ToString("yyyyMMdd") + "\\Cam1";
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            string fName = Dir + "\\" + DateTime.Now.ToString("HHmmss.fff") + ".jpg";
            HOperatorSet.WriteImage(h1Save, "jpg", 0, fName);
            AddToLog("Kaydetme-1", fName + " Kaydedildi", 9, Color.Gray);
        }

        private void SaveErrImages()
        {
            if (cam1Ready)
            {
                string Dir = System.Windows.Forms.Application.StartupPath + "\\Images\\VisionErr\\" + DateTime.Now.ToString("yyyyMMdd") + "\\Cam1";
                if (!Directory.Exists(Dir))
                {
                    Directory.CreateDirectory(Dir);
                }
                string fName = Dir + "\\E_" + DateTime.Now.ToString("HHmmss.fff") + ".bmp";
                HOperatorSet.WriteImage(ho_Image1, "bmp", 0, fName);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            GrabImage(true);
            Tile(true);
            DoControl();

        } // gÖRÜNTÜ ALMA

        private void button13_Click(object sender, EventArgs e)
        {
            if (spPLC.IsOpen)
                spPLC.Close();

            System.Windows.Forms.Application.Exit();
        } // KAPATMA

        private void button22_Click(object sender, EventArgs e)
        {

        }

        private void button25_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        private void tbLive_Click(object sender, EventArgs e)
        {

            tmrLive.Enabled = true;
            c1p.Checked = !c1p.Checked;
            if (c1p.Checked)
            {
                tbLive.BackColor = Color.ForestGreen;
            }
            else
            {
                tbLive.BackColor = Color.White;
            }
        }

        private void tmrLive_Tick(object sender, EventArgs e)
        {

            //if (c1p.Checked)
            //{
            //    tmrLive.Enabled = false;
            //    bool b = GrabImage(true);
            //    if (b)
            //    {
            //        Tile(true);
            //        //DoControl();
            //        // SaveCurImages();
            //        //if (bAuto.Checked)
            //        //    FindSpacers();
            //    }
            //    tmrLive.Enabled = c1p.Checked;
            //}
        }

        private void button18_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + "\\ProjectsAdd\\Ekran Kaydet.exe");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + "\\ProjectsAdd\\AppManagement.exe");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenSerialPort();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CloseSerialPort();
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            ReadPLC();
        }

        private void DplcOku(NumericUpDown n1, out string s)
        {
            s = "";
            bool b;
            int d;
            string rName = "M";
            string giden, gelen;
            string val;
            byte bNo = (byte)n1.Value;
            bool rOK = false;
            lbmerr.Text = "";
            rOK = Read_Single_D_Register(bNo, out giden, out gelen, out d);
            rName = "D";
            val = d.ToString();
            s = val;
            GiveMessage(rName + bNo.ToString() + "=" + val, rOK);
        }

        private bool MplcOku(NumericUpDown n1)
        {
           bool ret = false;
        
            bool b;
            string rName = "M";
            string giden, gelen;
            string val;
            byte bNo = (byte)n1.Value;
            bool rOK = false;
            lbmerr.Text = "";
            rOK = Read_Single_M_Register(bNo, out giden, out gelen, out b);
            val = b.ToString();
            bool c=Convert.ToBoolean(val);
            if (c)
            {
                ret = true;
            }
            return ret;
            //GiveMessage(rName + bNo.ToString() + "=" + val, rOK);
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            bool b;
            int d;
            string rName = "M";
            string giden, gelen;
            string val;
            byte bNo = (byte)cbCount_one.Value;
            bool rOK = false;
            lbmerr.Text = "";
            if (cbRegister.SelectedIndex == 0)
            {
                rOK = Read_Single_M_Register(bNo, out giden, out gelen, out b);
                val = b.ToString();
            }
            else if (cbRegister.SelectedIndex == 1)
            {
                rOK = Read_Single_X_Register(bNo, out giden, out gelen, out b);
                rName = "X";
                val = b.ToString();
            }
            else
            {
                rOK = Read_Single_D_Register(bNo, out giden, out gelen, out d);
                rName = "D";
                val = d.ToString();
            }

            txResp.Text = gelen;
            txSend.Text = giden;

            if (rOK)
                GiveMessage(rName + bNo.ToString() + "=" + val, rOK);
            else
            {
                txResp.ForeColor = rOK ? Color.Green : Color.Red;
                if (!rOK)
                {
                    if (gelen.Length > 6)
                    {
                        string errCode = gelen.Substring(5, 2);
                        string errDesc = errCode;
                        if (errCode.Equals("01")) errDesc = "Illegal Command Code";
                        else if (errCode.Equals("02")) errDesc = "Illegal Device address";
                        else if (errCode.Equals("03")) errDesc = "Illegal Device Value";
                        else if (errCode.Equals("07")) errDesc = "CheckSum Error, Illegal command messages";
                        lbmerr.Visible = true;
                        lbmerr.Text = errDesc;
                    }
                }

            }

        }
        private void button7_Click_1(object sender, EventArgs e)
        {
            string giden, cevap;
            byte bNo = (byte)cbCount_one.Value;
            bool b = Clear_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
            if (b) button8_Click_1(sender, e);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            string giden, cevap;
            byte bNo = (byte)cbCount_one.Value;

            bool b = Set_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
            if (b) button8_Click_1(sender, e);
        }

        private void SetMPLC(NumericUpDown n1)
        {
            string giden, cevap;
            byte bNo = (byte)n1.Value;

            bool b = Set_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
        }

        private void ResetMPLC(NumericUpDown n1)
        {
            string giden, cevap;
            byte bNo = (byte)n1.Value;
            bool b = Clear_Single_M_Register(bNo, out giden, out cevap);
            txResp.Text = cevap;
            txSend.Text = giden;
        }

        private void bt_serisave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void BtnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button21_Click(object sender, EventArgs e)
        {
 
            SetMPLC(zone_shot_count_one);
            SetDPLC(zone_size_count_one, "300");
            ResetMPLC(zone_shot_count_one);
        }

        private void tabPage7_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            DplcOku(bnt_count_one, out string myStr);  // Modüler Bant Okuma
            if (myStr.Length > 3)
            {
                string sonKarakter = myStr.Substring(0, 2);
                txt_modular_spd.Text = sonKarakter;
            }
            DplcOku(bnt_count_two, out string myStr2);  // ayıklama Bant Okuma
            if (myStr2.Length > 3)
            {
                string sonKarakter2 = myStr2.Substring(0, 2);
                txt_sorting_spd.Text = sonKarakter2;
            }
            else
            {
                txt_sorting_spd.Text = "0";
            }

            groupBox2.Visible = true;
            SayıCark = 1;
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            DplcOku(bnt_count_one, out string myStr);  // Modüler Bant Okuma
            if (myStr.Length > 3)
            {
                string sonKarakter = myStr.Substring(0, 2);
                txt_modular_spd.Text = sonKarakter;
            }
            DplcOku(bnt_count_two, out string myStr2);  // ayıklama Bant Okuma
            if (myStr2.Length > 3)
            {
                string sonKarakter2 = myStr2.Substring(0, 2);
                txt_sorting_spd.Text = sonKarakter2;
            }
            else
            {
                txt_sorting_spd.Text = "0";
            }

            groupBox2.Visible = true;
            SayıCark = 2;
        }

        private void textBox4_MouseClick(object sender, MouseEventArgs e)
        {
            //groupBox6.Visible = true;
        }

        private void textBox3_MouseClick(object sender, MouseEventArgs e)
        {
            //groupBox6.Visible = true;
        }

        private void textBox6_MouseClick(object sender, MouseEventArgs e)
        {
            //groupBox6.Visible = true;
        }

        private void textBox5_MouseClick(object sender, MouseEventArgs e)
        {
            //groupBox6.Visible = true;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            //groupBox6.Visible = false;
            SaveSettings();
        }

        private void button38_Click(object sender, EventArgs e)
        {
            //groupBox2.Visible = false;
            SayıCark = 0;
        }

        private void button3_Click_2(object sender, EventArgs e)
        {

        }

        private void button40_Click(object sender, EventArgs e)
        {
            SetMPLC(zone_shot_count_one);
        }

        private void button41_Click(object sender, EventArgs e)
        {
            SetMPLC(zone_shot_count_two);
        }

        private void button42_Click(object sender, EventArgs e)
        {
            SetMPLC(zone_shot_count_three);
        }

        private void button43_Click(object sender, EventArgs e)
        {
            ResetMPLC(zone_shot_count_one);
        }

        private void button44_Click(object sender, EventArgs e)
        {
            ResetMPLC(zone_shot_count_two);
        }

        private void button45_Click(object sender, EventArgs e)
        {
            ResetMPLC(zone_shot_count_three);
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void button47_Click(object sender, EventArgs e)
        {
            string Hız = "0";
            if (txt_modular_spd.Text == "" && txt_modular_spd.Text == null)
            {
                MessageBox.Show("Değer Boş Olamaz");
            }
            else
            {
                int Value = Convert.ToInt32(txt_modular_spd.Text);

                if (Value > 60)
                {
                    Hız = "6000";
                }
                else
                {
                    Hız = Value.ToString() + "00";
                }
                SetDPLC(bnt_count_one, Hız);
            }
        }

        private void Btn_Password(object sender, EventArgs e)
        {
            System.Windows.Forms.Button button = sender as System.Windows.Forms.Button;

            if (sifre.Length < 2)
            {

                if (button == one_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "1";
                    }
                    else
                    {
                        sifre = sifre + "1";
                    }
                }
                else if (button == two_conv)

                {
                    if (sifre == "")
                    {
                        sifre = "2";
                    }
                    else
                    {
                        sifre = sifre + "2";
                    }
                }
                else if (button == three_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "3";
                    }
                    else
                    {
                        sifre = sifre + "3";
                    }
                }

                else if (button == four_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "4";
                    }
                    else
                    {
                        sifre = sifre + "4";
                    }
                }
                else if (button == five_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "5";
                    }
                    else
                    {
                        sifre = sifre + "5";
                    }
                }
                else if (button == six_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "6";
                    }
                    else
                    {
                        sifre = sifre + "6";
                    }
                }
                else if (button == seven_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "7";
                    }
                    else
                    {
                        sifre = sifre + "7";
                    }
                }
                else if (button == eight_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "8";
                    }
                    else
                    {
                        sifre = sifre + "8";
                    }
                }
                else if (button == nine_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "9";
                    }
                    else
                    {
                        sifre = sifre + "9";
                    }
                }

                else if (button == zero_conv)
                {
                    if (sifre == "")
                    {
                        sifre = "0";
                    }
                    else
                    {
                        sifre = sifre + "0";
                    }
                }
            }
            if (SayıCark == 1)
            {
                txt_modular_spd.Text = sifre;
            }
            if (SayıCark == 2)
            {
                txt_sorting_spd.Text = sifre;
            }
        }

        private void button46_Click(object sender, EventArgs e)
        {
            string Hız = "0";
            if (txt_sorting_spd.Text == "" && txt_sorting_spd.Text == null)
            {
                MessageBox.Show("Değer Boş Olamaz");
            }
            else
            {
                int Value = Convert.ToInt32(txt_sorting_spd.Text);

                if (Value > 60)
                {
                    Hız = "6000";
                }
                else
                {
                    Hız = Value.ToString() + "00";
                }
                SetDPLC(bnt_count_two, Hız);
            }
        }

        private void textBox1_Click(object sender, EventArgs e)
        {

        }

        private void button36_Click(object sender, EventArgs e)
        {
            sifre = "";
            if (SayıCark == 1)
            {
                txt_modular_spd.Text = sifre;
            }
            if (SayıCark == 2)
            {
                txt_sorting_spd.Text = sifre;
            }
        }

 

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("keyboard.exe");

        }


        private void TimerElapsed4(object sender, ElapsedEventArgs e)
        {
            timer4.Enabled = false;
            try
            {

                bool b = GrabImage(true);
                if (b)
                {
                    label4.Invoke(new System.Action(() => Tile(true)));
                    label4.Invoke(new System.Action(() => DoControl()));
                    if (cbAuto.Checked)
                    {
                        label4.Invoke(new System.Action(() => SaveCurImages(TiledImage, 1)));
                    }
                  
                }
                else
                {

                    SistemCalisma = MplcOku(working_control_count);
                    AcilStop = MplcOku(numericUpDown1);
                    DriverError = MplcOku(numericUpDown2);
                    if (SistemCalisma == false)
                    {
                        //label4.Invoke(new System.Action(() => timer2.Enabled = false));

                    }
                    else
                    {
                        //label4.Invoke(new System.Action(() => timer2.Enabled = true));

                    }
                    if (AcilStop)
                    {
                        tabControl1.Invoke(new System.Action(() => tabControl1.SelectedIndex = 6));
                        label3.Invoke(new System.Action(() => label3.Visible = true));
                        label4.Invoke(new System.Action(() => label4.Visible = false));
                    }

                    if (DriverError)
                    {
                        tabControl1.Invoke(new System.Action(() => tabControl1.SelectedIndex = 6));
                        label4.Invoke(new System.Action(() => label4.Visible = true));
                        label3.Invoke(new System.Action(() => label3.Visible = false));
                    }
                }
            }
            catch(Exception ex) 
            {
            }
            timer4.Enabled = true;
        }

        private void lblPos2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            tabControl1.Invoke(new System.Action(() => tabControl1.SelectedIndex = 0));
        }


        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
          
            SetMPLC(zone_shot_count_one);
              SetDPLC(zone_size_count_one, H1.ToString());
            ResetMPLC(zone_shot_count_one);
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
          
            SetMPLC(zone_shot_count_three);
            SetDPLC(zone_size_count_three, H3.ToString());
            ResetMPLC(zone_shot_count_three);
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
           
            SetMPLC(zone_shot_count_two);
            SetDPLC(zone_size_count_two, H2.ToString());
            ResetMPLC(zone_shot_count_two);
        }

        private void label6_Click(object sender, EventArgs e)
        {
            label6.BackColor = label6.BackColor == Color.Green ? Color.Red : Color.Green;

        }

        private void label7_Click(object sender, EventArgs e)
        {
            label7.BackColor = label7.BackColor == Color.Green ? Color.Red : Color.Green;
        }

        private void label10_Click(object sender, EventArgs e)
        {
            label10.BackColor = label10.BackColor == Color.Green ? Color.Red : Color.Green;
        }

        private void label9_Click(object sender, EventArgs e)
        {
            label9.BackColor = label9.BackColor == Color.Green ? Color.Red : Color.Green;
        }



    }
}