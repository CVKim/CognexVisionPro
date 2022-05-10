//*****************************************************************************
// Copyright (C) 2011 Cognex Corporation
//
// Subject to Cognex Corporation's terms and conditions and license
// agreement, you are authorized to use and modify this source code in
// any way you find useful, provided the Software and/or the modified
// Software is used solely in conjunction with a Cognex Machine Vision
// System.  Furthermore you acknowledge and agree that Cognex has no
// warranty, obligations or liability for your use of the Software.
//*****************************************************************************
// This sample program is designed to illustrate certain VisionPro
// features or techniques in the simplest way possible. It is not
// intended as the framework for a complete application. In particular,
// the sample program may not provide proper error handling, event
// handling, cleanup, repeatability, and other mechanisms that a
// commercial quality application requires.
//
// This program assumes that you have some knowledge of C# and VisionPro
// programming.
//
// This sample program demonstrates the programmatic use of the VisionPro
// OCRMax Tool.
//


// This program employs two OCRMax tools to read two lines of text, a lot code
// and a date code. To find these strings on the image, it uses a PMAlign tool
// to identify some feature on the label, and set the region of interest
// accordingly. The  GUI guides the user through configuring the OCRMax tools
// and performing reads on an image database.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.Implementation;
using Cognex.VisionPro.OCRMax;
using Cognex.VisionPro.PMAlign;

namespace OCRMaxMultiLineFixture
{
    public partial class Form1 : Form
    {
        // OCR tool objects
        private CogOCRMaxTool mOCRMaxTool_Line1;
        private CogOCRMaxTool mOCRMaxTool_Line2;

        // Other required tools
        private CogPMAlignTool mPMAlignTool;
        private CogFixtureTool mFixtureTool;

        // Local variables
        private CogImageFile mImgLib;
        private int mImgLibPos = 0;
        private int mAlterNativeCharDisplayStartPos = 0;
        private CogOCRMaxPositionResult mPosResult = null;

        private CogImage8Grey mTrainImg;
        private CogOCRMaxSegmenterLineResult mSegLineResult;

        // Apriori known characters in the OCRMax train image
        private string mAlphabetString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789:/";
        private int[] mAlphabet;

        // Image paths. Will be extended with VPRO_ROOT directory
        private string mOCRTrainImgPath = @"OCRMax_train_couriernew.bmp";
        private string mPMAlignTrainImgPath = @"OCRMax_label.bmp";
        private string mImgLibPath = @"OCRMax_align_multiline.idb";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            // Display instructions
            txt_Description.Text = "";
            txt_Description.AppendText("Sample Description: ");
            txt_Description.AppendText("This application demonstrates the programmatic use of the Cognex VisionPro OCR Tool.");
            txt_Description.AppendText(Environment.NewLine);
            txt_Description.AppendText("Sample Usage: " + Environment.NewLine);
            txt_Description.AppendText("1) Open the train image" + Environment.NewLine);
            txt_Description.AppendText("2) Segment the image and match the train string with the extracted characters" + Environment.NewLine);
            txt_Description.AppendText("3) Add the characters to the font" + Environment.NewLine);
            txt_Description.AppendText("4) Train the tool" + Environment.NewLine);
            txt_Description.AppendText("5) Open image database" + Environment.NewLine);
            txt_Description.AppendText("6) Run the tool several times" + Environment.NewLine);
            txt_Description.AppendText("7) Check the alternative characters by enabling the Checkbox and selecting a character result");

            cogDisplay4.Image = null;

            // Read  environment variable with the image database location
            string VProPath = Environment.GetEnvironmentVariable("VPRO_ROOT"); // Visionpro 절대 경로
            if (VProPath == null)
            {
                MessageBox.Show("Could not read VPRO_ROOT environment variable.");
                System.Environment.Exit(0);
            }

            mAlphabet = CogOCRMaxChar.GetCharCodesFromString(mAlphabetString, "?");

            // Extend the filenames with the full path
            mOCRTrainImgPath = VProPath + "\\Images\\" + mOCRTrainImgPath;
            mPMAlignTrainImgPath = VProPath + "\\Images\\" + mPMAlignTrainImgPath;
            mImgLibPath = VProPath + "\\Images\\" + mImgLibPath;

            // Initialize tools
            mOCRMaxTool_Line1 = new CogOCRMaxTool();
            mOCRMaxTool_Line2 = new CogOCRMaxTool();
            mPMAlignTool = new CogPMAlignTool();
            mFixtureTool = new CogFixtureTool();
            // Initialize controls

            txt_FileName.Text = mOCRTrainImgPath;
            txt_CharNum.Text = "0";
            mOCRMaxTool_Line1.Classifier.Font.Changed += new CogChangedEventHandler(Font_Changed);
            cogDisplay1.Zoom = 2;
            cogDisplay2.Zoom = 2;
            cogDisplay3.Zoom = 2;
            
            try
            {
                // Configure and train the PatMax tool with the QR code image
                mPMAlignTool.InputImage = OpenSingleImageFile(mPMAlignTrainImgPath);
                mPMAlignTool.Pattern.TrainImage = mPMAlignTool.InputImage;
                CogRectangleAffine RoI = new CogRectangleAffine();
                RoI.SetOriginLengthsRotationSkew(18, 16, 50, 51, 0, 0);
                CogTransform2DLinear transf = new CogTransform2DLinear();
                transf.SetScalingsRotationsTranslation(1, 1, 0, 0, 90, 12);
                mPMAlignTool.Pattern.TrainRegion = RoI;
                mPMAlignTool.Pattern.Origin = transf;
                mPMAlignTool.Pattern.Train();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening PMAlign train image: " + ex.Message);
                System.Environment.Exit(0);
            }
        }

        // Update font library size text
        void Font_Changed(object sender, CogChangedEventArgs e)
        {
            txt_CharNum.Text = mOCRMaxTool_Line1.Classifier.Font.Count.ToString();
        }

        // Returns a CogImage8Grey when a single file is opened
        private CogImage8Grey OpenSingleImageFile(string path)
        {
            CogImageFile imgFile = new CogImageFile();
            try
            {
                imgFile.Open(path, CogImageFileModeConstants.Read);
                CogImage8Grey retImg = (CogImage8Grey)imgFile[0];
                return retImg;
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit("Could not load train image:" + Environment.NewLine + ex.Message, true);
                return null;
            }
            finally
            {
                imgFile.Close();
            }
        }

        // Error handler method
        private void DisplayErrorAndExit(string errorMsg, bool exit)
        {
            MessageBox.Show(errorMsg, "OCRMax sample error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (exit)
            {
                this.Close();
            }
        }

        // Open the train image for the OCRMax tool
        private void btn_OpenFile_Click(object sender, EventArgs e)
        {

            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowDialog();

            var FileName = openFileDialog1.FileName;

            mTrainImg = OpenSingleImageFile(FileName.ToString());
            if (mTrainImg == null)
            {
                DisplayErrorAndExit("Train image cannot be found!", false);
            }
            else
            {
                cogDisplay4.Image = mTrainImg;
                cogDisplay4.Fit();
            }
            
            // Set input image of the tool and the full image for RoI
            mOCRMaxTool_Line1.InputImage = mTrainImg;
            mOCRMaxTool_Line1.Region.FitToImage(mTrainImg, 1, 1);
            
            // Display the image in the CogRecordsDisplay
            cogRecordsDisplay.Subject = mOCRMaxTool_Line1.CreateCurrentRecord();

            // Load the alphabet into the TextBox
            txt_TrainString.Text = mAlphabetString;
            
            // Enable next step
            btn_ExtractChars.Enabled = true;
        }

        // Run the Segmenter and match the extracted characters with the apriori known train-string
        private void btn_ExtractChars_Click(object sender, EventArgs e)
        {
            // We use the first OCRMax tool to segment the image
            CogOCRMaxSegmenterParagraphResult segParagraphResult = null;
            try
            {
                // Change the minimum aspect of the searched characters from the default (0.8),
                // so that the tool properly segment ':' and '/' as well
                mOCRMaxTool_Line1.Segmenter.CharacterMinAspect = 1;
                mOCRMaxTool_Line1.Segmenter.CharacterMinWidth = 3;
                mOCRMaxTool_Line1.Segmenter.CharacterMaxWidth = 50;
                mOCRMaxTool_Line1.Segmenter.CharacterMinHeight = 3;
                mOCRMaxTool_Line1.Segmenter.CharacterMaxHeight = 50;

                // Run the Segmenter
                segParagraphResult = mOCRMaxTool_Line1.Segmenter.Execute(mTrainImg);
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit("Could not segment train image:" + Environment.NewLine + ex.Message, false);
                return;
            }

            // Get line result from paragraph result
            mSegLineResult = segParagraphResult[0];
            CogGraphicCollection characterGraphics = new CogGraphicCollection();

            try
            {
                // Create rectangles graphic labels to display the matched characters
                int counter = 0;
                foreach (CogOCRMaxSegmenterPositionResult segPosResult in mSegLineResult)
                {
                    CogGraphicLabel cgl = new CogGraphicLabel();

                    cgl.BackgroundColor = segPosResult.CellRect.Color;
                    cgl.Color = CogColorConstants.Black;
                    cgl.Interactive = false;

                    // Set the labels' text
                    string character;
                    if(counter < txt_TrainString.Text.Length)
                    {
                        character = txt_TrainString.Text[counter].ToString();
                    }
                    else
                    {
                        character = "?";
                    }
                    ++counter;

                    // Determine the labels' position
                    cgl.SetXYText(((segPosResult.CellRect.CornerXX + segPosResult.CellRect.CornerYX) / 2) + 3,
                                  segPosResult.CellRect.CornerYY + 60,
                                  character);

                    // Add bounding box and label
                    characterGraphics.Add(segPosResult.CellRect);
                    characterGraphics.Add(cgl);
                }
            
                // Modify the image record to display the labels in the CogRecordsDisplay
                CogRecord currentRecord = (CogRecord)mOCRMaxTool_Line1.CreateCurrentRecord();
                CogRecord charPosRecord = new CogRecord("Segmented characters",
                                                        typeof(CogGraphicCollection),
                                                        CogRecordUsageConstants.Temporary,
                                                        true,
                                                        characterGraphics,
                                                        "Segmented characters");

                currentRecord.SubRecords[0].SubRecords.Add(charPosRecord);
                cogRecordsDisplay.Subject = currentRecord;
                
                // Enable next step
                btn_AddFonts.Enabled = true;
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit(ex.Message, false);
            }
            
        }

        // Add the segmented fonts to the Classifier's library
        private void btn_AddFonts_Click(object sender, EventArgs e)
        {
            try
            {
                int iC = 0;
                foreach (CogOCRMaxSegmenterPositionResult segPosResult in mSegLineResult)
                {
                    CogOCRMaxChar oC = segPosResult.Character;
                    oC.CharacterCode = mAlphabet[iC];
                    mOCRMaxTool_Line1.Classifier.Font.Add(oC);
                    iC++;
                }
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit("Could not add characters to the font: " + ex.Message, false);
            }

            if (mOCRMaxTool_Line1.Classifier.Font.Count > 0)
            {
                // Prevent adding the same fonts multiple times
                btn_AddFonts.Enabled = false;

                // Enable next step
                btn_TrainTool.Enabled = true;
            }
        }

        // Train the OCRMax tool (only the first one, the second will use the same font-library
        private void btn_TrainTool_Click(object sender, EventArgs e)
        {
            try
            {
                mOCRMaxTool_Line1.Classifier.Train();
                if (!mOCRMaxTool_Line1.Classifier.Trained)
                {
                    throw new Exception("unknown error");
                }
                lbl_Status.Text = "Tool trained.";
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit("Could not train classifier: " + ex.Message, false);
            }

            // Enable next step
            btn_LoadImgFile.Enabled = true;
        }

        // Load image libary to work on
        private void btn_LoadImgFile_Click(object sender, EventArgs e)
        {
            // Initialize reading position
            mImgLibPos = 0;
            
            // Disable the previous buttons
            btn_OpenFile.Enabled = false;
            btn_ExtractChars.Enabled = false;
            btn_AddFonts.Enabled = false;
            btn_TrainTool.Enabled = false;
            
            // Open image database
            if (mImgLib == null)
            {
                mImgLib = new CogImageFile();
            }
            else if (mImgLib.OpenMode != CogImageFileModeConstants.Closed)
            {
                mImgLib.Close();
            }
            mImgLib.Open(mImgLibPath, CogImageFileModeConstants.Read);


            if (mImgLib.Count == 0)
            {
                DisplayErrorAndExit("Image DB empty!", true);
            }

            mOCRMaxTool_Line1.InputImage = mImgLib[0];
            mOCRMaxTool_Line1.Region.FitToImage(mOCRMaxTool_Line1.InputImage, 1, 1);
            cogRecordsDisplay.Subject = (CogRecord)mOCRMaxTool_Line1.CreateCurrentRecord();

            // Configure OCR tools
            CogRectangleAffine RoI1 = new CogRectangleAffine();
            CogRectangleAffine RoI2 = new CogRectangleAffine();

            //RoI1.SetOriginLengthsRotationSkew(0, 0, 300, 40, 0, 0);
            //RoI2.SetOriginLengthsRotationSkew(0, 44, 410, 40, 0, 0);

            RoI1.SetOriginLengthsRotationSkew(0, 0, 300, 40, 0, 0);
            RoI2.SetOriginLengthsRotationSkew(0, 44, 410, 40, 0, 0);

            mOCRMaxTool_Line1.Region = RoI1;
            mOCRMaxTool_Line2.Region = RoI2;

            btn_RunTool.Enabled = true;
        }

        // Run the tool-chain
        private void btn_RunTool_Click(object sender, EventArgs e)
        {
            // Unsubscribing from label changed events from previous run
            UnsubscribeLabelEvents();

            // These assignments function as links in Quickbuild
            // The order of execution: PatMax, Fixture, OCRMax1, OCRMax2
            ICogImage icogimage = null;
            try
            {
                using (CogImageFileTool ImagefileTool = new CogImageFileTool())
                {
                    ImagefileTool.Operator.Open("C:\\ocr_test\\ocr.idb", CogImageFileModeConstants.Read);
                    ImagefileTool.Run();
                    icogimage = ImagefileTool.OutputImage as ICogImage;
                }
            }
            catch
            {

            }


            mPMAlignTool.InputImage = (CogImage8Grey)mImgLib[mImgLibPos];
            mPMAlignTool.Run();

            mFixtureTool.InputImage = (CogImage8Grey)mImgLib[mImgLibPos];
            mFixtureTool.RunParams.UnfixturedFromFixturedTransform = mPMAlignTool.Results[0].GetPose();
            mFixtureTool.Run();

            //mOCRMaxTool_Line1.InputImage = (CogImage8Grey)mImgLib[mImgLibPos];
            mOCRMaxTool_Line1.InputImage = mFixtureTool.OutputImage;
            mOCRMaxTool_Line1.Run();

            mOCRMaxTool_Line2.InputImage = mFixtureTool.OutputImage;
            mOCRMaxTool_Line2.Segmenter = mOCRMaxTool_Line1.Segmenter;
            mOCRMaxTool_Line2.Classifier.Font = mOCRMaxTool_Line1.Classifier.Font;
            mOCRMaxTool_Line2.Classifier.Train();
            
            mOCRMaxTool_Line2.Run();

            // Read next image from library, restart if finished
            mImgLibPos++;

            if (mImgLibPos == mImgLib.Count)
            {
                mImgLibPos = 0;
            }

            // Record structure:
            //      LastRunRecord           empty
            //          Subrecords[0]       CogImage8Grey
            //              Subrecords[0]   CogRectangleAffin (RoI)
            //              Subrecords[1]   CogGraphicCollection (CogCompositeGraphics with bounding boxes and labels)
            //
            // Add image subrecords of second OCRMax tool to the first tools record

            CogRecord compositeRecord = (CogRecord)mOCRMaxTool_Line1.CreateLastRunRecord();
            CogRecord secondRecord = (CogRecord)mOCRMaxTool_Line2.CreateLastRunRecord();

            compositeRecord.SubRecords[0].SubRecords.Add(secondRecord.SubRecords[0].SubRecords[0]);
            compositeRecord.SubRecords[0].SubRecords.Add(secondRecord.SubRecords[0].SubRecords[1]);

            // Set the subject of the display to our composite record
            cogRecordsDisplay.Subject = compositeRecord;

            // Hook a new event-handler to the CogCompositeGraphic objects to display alternative characters on click
            // Separate event-handlers are needed for the two lines, since the CogOCRMax.LineResult must be accessed
            CogGraphicCollection graphColl;
            graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[1].Content as CogGraphicCollection;
            if(graphColl != null)
            {
                foreach(ICogGraphic graphic in graphColl)
                {
                    CogCompositeShape compositeShape = graphic as CogCompositeShape;
                    if(compositeShape != null)
                    {
                        compositeShape.Changed += new CogChangedEventHandler(CompShape_Line1_Changed);
                        
                        // Remove tooltip containing the same information as the alternative char panel
                        ((CogRectangleAffine)compositeShape.Shapes[0]).TipText = String.Empty;
                        ((CogGraphicLabel)compositeShape.Shapes[0].Children[0]).TipText = String.Empty;
                    }
                }
            }

            graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[3].Content as CogGraphicCollection;
            if (graphColl != null)
            {
                foreach (ICogGraphic graphic in graphColl)
                {
                    CogCompositeShape compositeShape = graphic as CogCompositeShape;
                    if (compositeShape != null)
                    {
                        compositeShape.Changed += new CogChangedEventHandler(CompShape_Line2_Changed);
                        
                        // Remove tooltip containing the same information as the alternative char panel
                        ((CogRectangleAffine)compositeShape.Shapes[0]).TipText = String.Empty;
                        ((CogGraphicLabel)compositeShape.Shapes[0].Children[0]).TipText = String.Empty;
                    }
                }
            }

            // Clear the alternative char panel
            ClearAlternativeChars();
        }

        private void UnsubscribeLabelEvents()
        {
            CogGraphicCollection graphColl;
            try
            {
                if (cogRecordsDisplay.Subject.SubRecords[0].SubRecords.Count > 1)
                {
                    graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[1].Content as CogGraphicCollection;
                    if (graphColl != null)
                    {
                        foreach (ICogGraphic graphic in graphColl)
                        {
                            CogCompositeShape compositeShape = graphic as CogCompositeShape;
                            if (compositeShape != null)
                            {
                                compositeShape.Changed -= new CogChangedEventHandler(CompShape_Line1_Changed);
                            }
                        }
                    }
                }

                if (cogRecordsDisplay.Subject.SubRecords[0].SubRecords.Count > 3)
                {
                    graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[3].Content as CogGraphicCollection;
                    if (graphColl != null)
                    {
                        foreach (ICogGraphic graphic in graphColl)
                        {
                            CogCompositeShape compositeShape = graphic as CogCompositeShape;
                            if (compositeShape != null)
                            {
                                compositeShape.Changed -= new CogChangedEventHandler(CompShape_Line2_Changed);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                DisplayErrorAndExit("Error unsubscribing from label events", false);
            }
        }

        // OCRMax Line1 character label changed
        private void CompShape_Line1_Changed(object sender, CogChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            try
            {
                if (sender.GetType() == typeof(CogCompositeShape) && ((CogCompositeShape)sender).Selected)
                {

                    // Reset alt-char display offset
                    mAlterNativeCharDisplayStartPos = 0;
                    CogGraphicCollection graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[1].Content as CogGraphicCollection;
                    if (graphColl == null)
                    {
                        return;
                    }

                    // Find the clicked character
                    int ID = -1;
                    for (int i = 0; i < graphColl.Count; ++i)
                    {
                        if (sender == graphColl[i])
                        {
                            ID = i;
                        }
                    }

                    if (ID == -1)
                    {
                        return;
                    }
                    else
                    {
                        // Update the alternative character display
                        mPosResult = mOCRMaxTool_Line1.LineResult[ID];
                        UpdateAlternativeChars();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Tried to access the object after disposal. This might happen when
                // the CogCompositeShapes still fire their changed events while being disposed.
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit(ex.Message, false);
            }
        }

        // OCRMax Line2 character label changed
        private void CompShape_Line2_Changed(object sender, CogChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            try
            {
                if (sender.GetType() == typeof(CogCompositeShape) && ((CogCompositeShape)sender).Selected)
                {
                    // Reset alt-char display offset

                    mAlterNativeCharDisplayStartPos = 0;
                    CogGraphicCollection graphColl = cogRecordsDisplay.Subject.SubRecords[0].SubRecords[3].Content as CogGraphicCollection;
                    if (graphColl == null)
                    {
                        return;
                    }

                    // Find the clicked character
                    int ID = -1;
                    for (int i = 0; i < graphColl.Count; ++i)
                    {
                        if (sender == graphColl[i])
                        {
                            ID = i;
                        }
                    }

                    if (ID == -1)
                    {
                        return;
                    }
                    else
                    {
                        // Update the alternative character display
                        mPosResult = mOCRMaxTool_Line2.LineResult[ID];
                        UpdateAlternativeChars();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Tried to access the object after disposal
            }
            catch (Exception ex)
            {
                DisplayErrorAndExit("Error handling imagerecords: " + ex.Message, false);
            }
        }

        // Refresh the content of the alternative char panel
        private void UpdateAlternativeChars()
        {
            // If no character is selected
            if (mPosResult == null)
            {
                return;
            }

            // Display null 처리 후, 시작
            cogDisplay1.Image = null;
            cogDisplay2.Image = null;
            cogDisplay3.Image = null;

            txt_Char1.Text = "";
            txt_Char2.Text = "";
            txt_Char3.Text = "";

            txt_Score1.Text = "";
            txt_Score2.Text = "";
            txt_Score3.Text = "";

            // Determine the range of displayed alternative characters
            int startChar = mAlterNativeCharDisplayStartPos;
            int altCharNum = mPosResult.AlternativeCharacters.Count;

            // Refresh Textboxes and CogDisplays
            txt_Char0.Text = mPosResult.GetCharacter().Key.GetString();
            txt_Score0.Text = mPosResult.Score.ToString();

            // If: the characters to be displayed are out of range clear the controls,
            // else: set their content
            if (startChar >= altCharNum)
            {
                txt_Char1.Text = String.Empty;
                txt_Score1.Text = String.Empty;
                cogDisplay1.Image = null;
            }
            else
            {
                if (altCharNum < 3) return;
                
                txt_Char1.Text = mPosResult.AlternativeCharacters[startChar].Key.GetString();
                txt_Score1.Text = mPosResult.AlternativeCharacters[startChar].Score.ToString();
                cogDisplay1.Image = mOCRMaxTool_Line1.Classifier.Font[mPosResult.AlternativeCharacters[startChar].Key].Image;
            }

            if (startChar + 1 >= altCharNum)
            {
                txt_Char2.Text = String.Empty;
                txt_Score2.Text = String.Empty;
                cogDisplay2.Image = null;
            }
            else
            {
                txt_Char2.Text = mPosResult.AlternativeCharacters[startChar + 1].Key.GetString();
                txt_Score2.Text = mPosResult.AlternativeCharacters[startChar + 1].Score.ToString();
                cogDisplay2.Image = mOCRMaxTool_Line1.Classifier.Font[mPosResult.AlternativeCharacters[startChar + 1].Key].Image;
            }

            if (startChar + 2 >= altCharNum)
            {
                txt_Char3.Text = String.Empty;
                txt_Score3.Text = String.Empty;
                cogDisplay3.Image = null;
            }
            else
            {
                txt_Char3.Text = mPosResult.AlternativeCharacters[startChar + 2].Key.GetString();
                txt_Score3.Text = mPosResult.AlternativeCharacters[startChar + 2].Score.ToString();
                cogDisplay3.Image = mOCRMaxTool_Line1.Classifier.Font[mPosResult.AlternativeCharacters[startChar + 2].Key].Image;
            }
        }

        // Clears the Textboxes and CogDisplays in the alternative char panel
        private void ClearAlternativeChars()
        {
            txt_Char0.Text = String.Empty;
            txt_Char1.Text = String.Empty;
            txt_Char2.Text = String.Empty;
            txt_Char3.Text = String.Empty;
            txt_Score0.Text = String.Empty;
            txt_Score1.Text = String.Empty;
            txt_Score2.Text = String.Empty;
            txt_Score3.Text = String.Empty;
            cogDisplay1.Image = null;
            cogDisplay2.Image = null;
            cogDisplay3.Image = null;
        }
        // Display and hide the alternative characters display panel
        private void ckb_DisplayAltChars_CheckedChanged(object sender, EventArgs e)
        {
            if (ckb_DisplayAltChars.Checked)
            {
                pnl_DisplayPanel.Visible = true;
            }
            else
            {
                pnl_DisplayPanel.Visible = false;
            }
        }

        // Change the range of displayed alternative characters by +1
        private void btn_Up_Click(object sender, EventArgs e)
        {
            if (mPosResult == null)
            {
                return;
            }
            mAlterNativeCharDisplayStartPos++;

            // Prevent negative indexes
            mAlterNativeCharDisplayStartPos = Math.Min(mPosResult.AlternativeCharacters.Count - 3, mAlterNativeCharDisplayStartPos);

            // Update labels
            lbl_AltChar1.Text = (mAlterNativeCharDisplayStartPos + 1).ToString() + ".";
            lbl_AltChar2.Text = (mAlterNativeCharDisplayStartPos + 2).ToString() + ".";
            lbl_AltChar3.Text = (mAlterNativeCharDisplayStartPos + 3).ToString() + ".";

            // Update displayed alternative characters
            UpdateAlternativeChars();
        }

        // Change the range of displayed alternative characters by -1
        private void btn_Down_Click(object sender, EventArgs e)
        {
            if (mPosResult == null)
            {
                return;
            }
            mAlterNativeCharDisplayStartPos--;

            // Prevent negative indexes
            mAlterNativeCharDisplayStartPos = Math.Max(0, mAlterNativeCharDisplayStartPos);

            // Update labels
            lbl_AltChar1.Text = (mAlterNativeCharDisplayStartPos + 1).ToString() + ".";
            lbl_AltChar2.Text = (mAlterNativeCharDisplayStartPos + 2).ToString() + ".";
            lbl_AltChar3.Text = (mAlterNativeCharDisplayStartPos + 3).ToString() + ".";

            // Update displayed alternative characters
            UpdateAlternativeChars();
        }

        private void btn_Clear_Click(object sender, EventArgs e)
        {
            this.cogRecordsDisplay.Subject = null;
        }

        // Close the image library on exit
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mImgLib != null)
            {
                mImgLib.Close();
            }
        }

        private void btn_clear_Click_1(object sender, EventArgs e)
        {
            this.cogRecordsDisplay.Subject = null;
        }

        private void lbl_AltChar1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowDialog();

            var FileName = openFileDialog1.FileName;

            CogImageFile IcogImageFile = null;

            ICogImage icogimage = null;
            try
            {
                using (CogImageFileTool ImagefileTool = new CogImageFileTool())
                {
                    ImagefileTool.Operator.Open(FileName, CogImageFileModeConstants.Read);
                    ImagefileTool.Run();
                    
                    IcogImageFile = ImagefileTool.OutputImage as CogImageFile;

                    //icogimage = ImagefileTool.OutputImage as ICogImage;
                }
            }
            catch
            {

            }

        }
    }
}
