using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DATN
{
    public partial class Form1 : Form
    {
        private SerialPort serialCommunication;
        private DHParameter[] dhParameters;
        private double[,] T_06;
        double d1 = 162.25;
        double a2 = 207.7;
        double a3 = 135.95;
        double d6 = 135.5;

        public Form1()
        {
            InitializeComponent();
            InitializeDHParameters();
            InitializeUI();
            InitializeDefaultValues();
            InitialSerialPort();
            InitialProgram();
        }

        private void InitialProgram()
        {
            //Initial Invisible Panels
            comboBox1.SelectedItem = "Simple";
            DetailedSelection.SelectedItem = "Joints";
            ManOrAuto.SelectedItem = "AUTO";
        }

        private void InitialSerialPort()
        {
            serialCommunication = new SerialPort("COM4",9600);
            serialCommunication.Open();
        }


        private void InitializeUI()
        {
            // Gán sự kiện chung cho tất cả TextBox
            J1TextBox.TextChanged += J1TextBox_TextChanged;
            J2TextBox.TextChanged += J2TextBox_TextChanged;
            J3TextBox.TextChanged += J3TextBox_TextChanged;
            J4TextBox.TextChanged += J4TextBox_TextChanged;
            J5TextBox.TextChanged += J5TextBox_TextChanged;
            J6TextBox.TextChanged += J6TextBox_TextChanged;
            XTextBox.TextChanged += XTextBox_TextChanged;
            YTextBox.TextChanged += YTextBox_TextChanged;
            ZTextBox.TextChanged += ZTextBox_TextChanged;
            ATextBox.TextChanged += ATextBox_TextChanged;
            BTextBox.TextChanged += BTextBox_TextChanged;
            CTextBox.TextChanged += CTextBox_TextChanged;

            // Gán sự kiện chung cho tất cả TrackBar
            J1Bar.Scroll += J1Bar_Scroll;
            J2Bar.Scroll += J2Bar_Scroll;
            J3Bar.Scroll += J3Bar_Scroll;
            J4Bar.Scroll += J4Bar_Scroll;
            J5Bar.Scroll += J5Bar_Scroll;
            J6Bar.Scroll += J6Bar_Scroll;
            XBar.Scroll += XBar_Scroll;
            YBar.Scroll += YBar_Scroll;
            ZBar.Scroll += ZBar_Scroll;
            ABar.Scroll += ABar_Scroll;
            BBar.Scroll += BBar_Scroll;
            CBar.Scroll += CBar_Scroll;
        }

        private void InitializeDefaultValues()
        {
            // Khởi tạo các giá trị góc J1 đến J6 đều bằng 0
            J1TextBox.Text = "-59";
            J2TextBox.Text = "178";
            J3TextBox.Text = "-20";
            J4TextBox.Text = "-3";
            J5TextBox.Text = "200";
            J6TextBox.Text = "180";

            // Cập nhật giá trị cho các thanh trượt (ScrollBars) tương ứng
            J1Bar.Value = -59;
            J2Bar.Value = 178;
            J3Bar.Value = -20;
            J4Bar.Value = -3;
            J5Bar.Value = 200;
            J6Bar.Value = 180;

            // Gọi hàm CalculateForwardKinematics để tính toán và cập nhật giá trị XYZABC
            CalculateForwardKinematics();
        }


        private void InitializeDHParameters()
        {

            dhParameters = new DHParameter[6]
            {

                new DHParameter(J1Bar.Value, 90, 0, d1),
                new DHParameter(J2Bar.Value, 0, a2, 0),
                new DHParameter(J3Bar.Value, 0, a3, 0),
                new DHParameter(J4Bar.Value, -90, 0, 0),
                new DHParameter(J5Bar.Value, 90, 0, 0),
                new DHParameter(J6Bar.Value, 0, 0, d6)
            };
        }

        private void SendCommand(string command, float value)
        {
            string message = $"{command}:{value};";
            serialCommunication.WriteLine(message);
        }

        private void SendAllJValues()
        {
            // Gửi các giá trị từ J1 đến J6
            SendCommand("1", J1Bar.Value);
            SendCommand("2", J2Bar.Value);
            SendCommand("3", J3Bar.Value);
            SendCommand("4", J4Bar.Value);
            SendCommand("5", J5Bar.Value);
            SendCommand("6", J6Bar.Value);
        }

        private void SetTargetPostion(double d_X,double d_Y,double d_Z,double d_A,double d_B,double d_C)
        {
            XBar.Value = (int)d_X;
            YBar.Value = (int)d_Y;
            ZBar.Value = (int)d_Z;
            ABar.Value = (int)d_A;
            BBar.Value = (int)d_B;
            CBar.Value = (int)d_C;
            XTextBox.Text = d_X.ToString();
            YTextBox.Text = d_Y.ToString();
            ZTextBox.Text = d_Z.ToString();
            ATextBox.Text = d_A.ToString();
            BTextBox.Text = d_B.ToString();
            CTextBox.Text = d_C.ToString();

            CalculateInverseKinematics();
        }

        private void ClickAdjustValue(System.Windows.Forms.TextBox textBox, System.Windows.Forms.HScrollBar hScrollBar, int adjustment)
        {
            int newValue = hScrollBar.Value + adjustment;

            if (newValue >= hScrollBar.Minimum && newValue <= hScrollBar.Maximum)
            {
                hScrollBar.Value = newValue;
            }

            textBox.Text = newValue.ToString();
        }


        private void UpdateTextBoxValue(System.Windows.Forms.TextBox textBox, System.Windows.Forms.HScrollBar hScrollBar)
        {
            if (int.TryParse(textBox.Text, out int value))
            {
                if (value >= hScrollBar.Minimum && value <= hScrollBar.Maximum)
                {
                    hScrollBar.Value = value;
                }
            }
        }

        private void UpdateBarValue(System.Windows.Forms.TextBox textBox, System.Windows.Forms.HScrollBar hScrollBar)
        {
            textBox.Text = hScrollBar.Value.ToString();
        }

        private bool CompareValue(double Value, double Min , double Max)
        {
            if (Value > Min || Value < Max) {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CalculateForwardKinematics()
        {
           
            bool isForwardKinematicsActive = false;  // Đánh dấu rằng tính toán thuận đang hoạt động

            while (!isForwardKinematicsActive)
            {
                // Cập nhật giá trị theta từ J1 đến J6
                dhParameters[0].Theta = J1Bar.Value;  // Lấy giá trị J1
                dhParameters[1].Theta = J2Bar.Value;  // Lấy giá trị J2
                dhParameters[2].Theta = J3Bar.Value;  // Lấy giá trị J3
                dhParameters[3].Theta = J4Bar.Value;  // Lấy giá trị J4
                dhParameters[4].Theta = J5Bar.Value;  // Lấy giá trị J5
                dhParameters[5].Theta = J6Bar.Value;  // Lấy giá trị J6

                // Khởi tạo ma trận biến đổi đầu tiên
                T_06 = DHParameter.CalculateTransformationMatrix(
                    dhParameters[0].Theta, dhParameters[0].Alpha, dhParameters[0].R, dhParameters[0].D);

                // Tính toán ma trận biến đổi T_06 bằng cách nhân các ma trận
                for (int i = 1; i < dhParameters.Length; i++)
                {
                    double[,] A = DHParameter.CalculateTransformationMatrix(
                        dhParameters[i].Theta, dhParameters[i].Alpha, dhParameters[i].R, dhParameters[i].D);
                    T_06 = DHParameter.MultiplyMatrices(T_06, A);
                }

                // Lấy giá trị vị trí cuối cùng Px, Py, Pz từ ma trận T_06
                double Px = T_06[0, 3], Py = T_06[1, 3], Pz = T_06[2, 3];

                // Nếu cần tính toán thêm cho góc ABC (thường là các góc quay Euler)
                double A_angle = Math.Atan2(T_06[2, 1], T_06[2, 2]);  // Cập nhật giá trị góc A
                double B_angle = Math.Atan2(-T_06[2, 0], Math.Sqrt(T_06[2, 1] * T_06[2, 1] + T_06[2, 2] * T_06[2, 2]));  // Cập nhật giá trị góc B
                double C_angle = Math.Atan2(T_06[1, 0], T_06[0, 0]);  // Cập nhật giá trị góc C

                if (CompareValue(Px, XBar.Minimum, XBar.Maximum) && CompareValue(Py, YBar.Minimum, YBar.Maximum) && CompareValue(Pz, ZBar.Minimum, ZBar.Maximum) && CompareValue(A_angle * 180 / Math.PI, ABar.Minimum, ABar.Maximum) && CompareValue(B_angle * 180 / Math.PI, BBar.Minimum, BBar.Maximum) && CompareValue(C_angle * 180 / Math.PI, CBar.Minimum, CBar.Maximum))
                {
                    A_angle = A_angle * 180 / Math.PI;
                    B_angle = B_angle * 180 / Math.PI;
                    C_angle = C_angle * 180 / Math.PI;
                    // Cập nhật giá trị cho TextBox
                    XTextBox.Text = Px.ToString("0.###");  // Cập nhật giá trị X
                    YTextBox.Text = Py.ToString("0.###");  // Cập nhật giá trị Y
                    ZTextBox.Text = Pz.ToString("0.###");  // Cập nhật giá trị Z
                    ATextBox.Text = A_angle.ToString("0.###");  // Cập nhật giá trị A
                    BTextBox.Text = B_angle.ToString("0.###");  // Cập nhật giá trị B
                    CTextBox.Text = C_angle.ToString("0.###");  // Cập nhật giá trị C

                    // Cập nhật giá trị cho Bar
                    XBar.Value = (int)Px;  // Cập nhật giá trị X
                    YBar.Value = (int)Py;  // Cập nhật giá trị Y
                    ZBar.Value = (int)Pz;  // Cập nhật giá trị Z
                    ABar.Value = (int)A_angle;  // Cập nhật giá trị A
                    BBar.Value = (int)B_angle;  // Cập nhật giá trị B
                    CBar.Value = (int)C_angle;  // Cập nhật giá trị C

                    isForwardKinematicsActive = true;  // Reset trạng thái khi hoàn thành
                }

            }
        }


        private void CalculateInverseKinematics()
        {
            bool isInverseKinematicsActive = false;

            while (!isInverseKinematicsActive)
            {
                // Cập nhật giá trị theta từ J1 đến J6
                double X = XBar.Value; // Lấy giá trị X
                double Y = YBar.Value;  // Lấy giá trị Y
                double Z = ZBar.Value;  // Lấy giá trị Z
                double A = (ABar.Value) * (Math.PI / 180);
                double B = (BBar.Value) * (Math.PI / 180);  // Lấy giá trị B
                double C = (CBar.Value) * (Math.PI / 180);  // Lấy giá trị C

                double[,] R_x = new double[3, 3]
                {
                    { 1, 0, 0 },
                    { 0, Math.Cos(A), -Math.Sin(A) },
                    { 0, Math.Sin(A), Math.Cos(A) }
                };

                double[,] R_y = new double[3, 3]
                {
                    { Math.Cos(B), 0, Math.Sin(B) },
                    { 0, 1, 0 },
                    { -Math.Sin(B), 0, Math.Cos(B) }
                };

                double[,] R_z = new double[3, 3]
                {
                    { Math.Cos(C), -Math.Sin(C), 0 },
                    { Math.Sin(C), Math.Cos(C), 0 },
                    { 0, 0, 1 }
                };


                double[,] R_zy = DHParameter.MultiplyMatrices(R_z, R_y);
                double[,] R_xyz = DHParameter.MultiplyMatrices(R_zy, R_x);

                double[,] W = new double[3, 1]
                {
                    {X - d6*R_xyz[0,2] }, {Y - d6*R_xyz[1,2] }, {Z - d6*R_xyz[2,2]}
                };

                double W_x = W[0, 0];
                double W_y = W[1, 0];
                double W_z = W[2, 0];


                double J1 = Math.Atan2(W_y,W_x);

                double r = Math.Sqrt(Math.Pow(W_x,2) + Math.Pow(W_y,2));
                double s = W_z - d1;
                double J3 = Math.Acos((Math.Pow(r,2) + Math.Pow(s,2) - Math.Pow(a2,2) - Math.Pow(a3,2)) / (2 * a2 * a3));
                double J2 = Math.Asin(((a2 + a3 * Math.Cos(J3)) * s - a3 * Math.Sin(J3) * r) / (Math.Pow(r,2) + Math.Pow(s,2)));

                double J4 = Math.Atan2(-(Math.Cos(J1)) * Math.Sin(J2 + J3) * R_xyz[0, 2] - (Math.Sin(J1)) * Math.Sin(J2 + J3) * R_xyz[1, 2] + Math.Cos(J2 + J3) * R_xyz[2, 2], Math.Cos(J1) * Math.Cos(J2 + J3) * R_xyz[0, 2] + Math.Sin(J1) * Math.Cos(J2 + J3) * R_xyz[1, 2] + Math.Sin(J2 + J3) * R_xyz[2, 2]);
                double J5 = Math.Atan2(Math.Sqrt(1 - (Math.Pow(Math.Sin(J1) * R_xyz[0,2]-Math.Cos(J1) * R_xyz[1, 2], 2))), Math.Sin(J1) * R_xyz[0, 2] - Math.Cos(J1) * R_xyz[1, 2]);
                double J6 = Math.Atan2(Math.Sin(J1) * R_xyz[0, 1] - Math.Cos(J1) * R_xyz[1, 1], -Math.Sin(J1) * R_xyz[0, 0] + Math.Cos(J1) * R_xyz[1, 1]);

                if (CompareValue(J1 * 180 / Math.PI, J1Bar.Minimum, J1Bar.Maximum) && CompareValue(J2 * 180 / Math.PI, J2Bar.Minimum, J2Bar.Maximum) && CompareValue(J3 * 180 / Math.PI, J3Bar.Minimum, J3Bar.Maximum) && CompareValue(J4 * 180 / Math.PI, J4Bar.Minimum, J4Bar.Maximum) && CompareValue(J5 * 180 / Math.PI, J5Bar.Minimum, J5Bar.Maximum) && CompareValue(J6 * 180 / Math.PI, J6Bar.Minimum, J6Bar.Maximum))
                {

                    J1 = J1 * 180 / Math.PI;
                    J2 = J2 * 180 / Math.PI;
                    J3 = J3 * 180 / Math.PI;
                    J4 = J4 * 180 / Math.PI;
                    J5 = J5 * 180 / Math.PI;
                    J6 = J6 * 180 / Math.PI;
                    // Cập nhật giá trị cho XYZTextBox
                    J1TextBox.Text = J1.ToString("0.###");  // Cập nhật giá trị J1
                    J2TextBox.Text = J2.ToString("0.###");  // Cập nhật giá trị J2
                    J3TextBox.Text = J3.ToString("0.###");  // Cập nhật giá trị J3
                    J4TextBox.Text = J4.ToString("0.###");  // Cập nhật giá trị J1
                    J5TextBox.Text = J5.ToString("0.###");  // Cập nhật giá trị J2
                    J6TextBox.Text = J6.ToString("0.###");  // Cập nhật giá trị J3

                    // Cập nhật giá trị cho J1 to J6 Bar
                    J1Bar.Value = (int)J1;  // Cập nhật giá trị J1
                    J2Bar.Value = (int)J2;  // Cập nhật giá trị J2
                    J3Bar.Value = (int)J3;  // Cập nhật giá trị J3
                    J4Bar.Value = (int)J4;  // Cập nhật giá trị J4
                    J5Bar.Value = (int)J5;  // Cập nhật giá trị J5
                    J6Bar.Value = (int)J6;  // Cập nhật giá trị J6

                    isInverseKinematicsActive = true; // Reset the flag

                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Simple")
            {
                SendCommand("Simple",2);
                SimpleMode.Visible = true;
                DetailedMode.Visible = false;
            }
            else if (comboBox1.SelectedItem.ToString() == "Detailed")
            {
                SendCommand("Detailed",2);
                SimpleMode.Visible = false;
                DetailedMode.Visible = true;
            }
        }

        private void J1TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J1TextBox, J1Bar);
        }

        private void J2TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J2TextBox, J2Bar);
        }

        private void J3TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J3TextBox, J3Bar);
        }

        private void J4TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J4TextBox, J4Bar);
        }

        private void J5TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J5TextBox, J5Bar);
        }

        private void J6TextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(J6TextBox, J6Bar);
        }

        private void XTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(XTextBox, XBar);
        }

        private void YTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(YTextBox, YBar);
        }

        private void ZTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(ZTextBox, ZBar);
        }

        private void ATextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(ATextBox, ABar);
        }

        private void BTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(BTextBox, BBar);
        }

        private void CTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxValue(CTextBox, CBar);
        }

        private void J1Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J1TextBox, J1Bar);
        }

        private void J2Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J2TextBox, J2Bar);
        }

        private void J3Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J3TextBox, J3Bar);
        }

        private void J4Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J4TextBox, J4Bar);
        }

        private void J5Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J5TextBox, J5Bar);
        }

        private void J6Bar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(J6TextBox, J6Bar);
        }


        private void XBar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(XTextBox, XBar);
        }

        private void YBar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(YTextBox, YBar);
        }

        private void ZBar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(ZTextBox, ZBar);
        }

        private void ABar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(ATextBox, ABar);
        }

        private void BBar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(BTextBox, BBar);
        }

        private void CBar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateBarValue(CTextBox, CBar);
        }

        private void J1ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J1TextBox, J1Bar,1);
        }

        private void J2ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J2TextBox, J2Bar, 1);
        }

        private void J3ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J3TextBox, J3Bar, 1);
        }

        private void J4ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J4TextBox, J4Bar, 1);
        }

        private void J5ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J5TextBox, J5Bar, 1);
        }

        private void J6ButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J6TextBox, J6Bar, 1);
        }

        private void XButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(XTextBox, XBar, 1);
        }

        private void YButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(YTextBox, YBar, 1);
        }

        private void ZButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(ZTextBox, ZBar, 1);
        }

        private void AButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(ATextBox, ABar, 1);
        }

        private void BButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(BTextBox, BBar, 1);
        }

        private void CButtonUp_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(CTextBox, CBar, 1);
        }

        private void J1ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J1TextBox, J1Bar, -1);
        }

        private void J2ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J2TextBox, J2Bar, -1);
        }

        private void J3ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J3TextBox, J3Bar, -1);
        }

        private void J4ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J4TextBox, J4Bar, -1);
        }

        private void J5ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J5TextBox, J5Bar, -1);
        }

        private void J6ButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(J6TextBox, J6Bar, -1);
        }

        private void XButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(XTextBox, XBar, -1);
        }

        private void YButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(YTextBox, YBar, -1);
        }

        private void ZButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(ZTextBox, ZBar, -1);
        }

        private void AButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(ATextBox, ABar, -1);
        }

        private void BButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(BTextBox, BBar, -1);
        }

        private void CButtonDown_Click(object sender, EventArgs e)
        {
            ClickAdjustValue(CTextBox, CBar, -1);
        }

        private void Pick_Click(object sender, EventArgs e)
        {
            SendCommand("G", 0);
        }

        private void Place_Click(object sender, EventArgs e)
        {
            SendCommand("D", 0);
        }

        private void barValue(int J1, int J2, int J3, int J4, int J5, int J6) {
            J1Bar.Value = J1;
            J2Bar.Value = J2;
            J3Bar.Value = J3;
            J4Bar.Value = J4;
            J5Bar.Value = J5;
            J6Bar.Value = J6;
        }

        private void Home_Click(object sender, EventArgs e)
        {
            SendCommand("H", 0);
            barValue(-59,178,-20,-3,200,180);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private void pos1_Click(object sender, EventArgs e)
        {
            SendCommand("P1", 0);
            barValue(114, 57, -18, 90, 130, 110);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);

        }

        private void pos2_Click(object sender, EventArgs e)
        {
            SendCommand("P2", 0);
            barValue(85, 57, -18, 90, 130, 80);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private void pos3_Click(object sender, EventArgs e)
        {
            SendCommand("P3", 0);
            barValue(63, 49, 3, 83, 140, 60);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private void posA_Click(object sender, EventArgs e)
        {
            SendCommand("PA", 0);
            barValue(-38, 42, -35, 40, 14, -80);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private void posB_Click(object sender, EventArgs e)
        {
            SendCommand("PB", 0);
            barValue(-9, 42, -35, 40, 14, -80);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private void posC_Click(object sender, EventArgs e)
        {
            SendCommand("PC", 0);
            barValue(37, 40, -22, 70, 22, -78);
            UpdateBarValue(J1TextBox, J1Bar);
            UpdateBarValue(J2TextBox, J2Bar);
            UpdateBarValue(J3TextBox, J3Bar);
            UpdateBarValue(J4TextBox, J4Bar);
            UpdateBarValue(J5TextBox, J5Bar);
            UpdateBarValue(J6TextBox, J6Bar);
        }

        private bool CheckAllJValues()
        {
            // Mảng các TextBox
            System.Windows.Forms.TextBox[] textBoxes = { J1TextBox, J2TextBox, J3TextBox, J4TextBox, J5TextBox, J6TextBox };
            // Mảng các HScrollBar
            System.Windows.Forms.HScrollBar[] hScrollBars = { J1Bar, J2Bar, J3Bar, J4Bar, J5Bar, J6Bar };
            // Mảng tên ngắn gọn
            string[] jointNames = { "J1", "J2", "J3", "J4", "J5", "J6" };

            // Danh sách để lưu trữ các thông báo lỗi
            List<string> errorMessages = new List<string>();

            for (int i = 0; i < textBoxes.Length; i++)
            {
                // Kiểm tra nếu giá trị nhập vào không phải số nguyên
                if (!int.TryParse(textBoxes[i].Text, out int value))
                {
                    errorMessages.Add($"Giá trị tại {jointNames[i]} không hợp lệ! Vui lòng nhập số nguyên.");
                }
                else if (value < hScrollBars[i].Minimum || value > hScrollBars[i].Maximum) // Giá trị nằm ngoài phạm vi
                {
                    errorMessages.Add($"Giá trị tại {jointNames[i]} phải nằm trong khoảng {hScrollBars[i].Minimum} và {hScrollBars[i].Maximum}.");
                }
            }

            // Nếu có lỗi, hiển thị toàn bộ thông báo
            if (errorMessages.Count > 0)
            {
                string allErrors = string.Join("\n", errorMessages);
                System.Windows.Forms.MessageBox.Show(allErrors, "Lỗi",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }


        private void Cal_Click(object sender, EventArgs e)
        {
            if (CheckAllJValues())
            {
                if (comboBox1.SelectedItem.ToString() == "Detailed")
                {
                    if (DetailedSelection.SelectedItem.ToString() == "Joints")
                    {
                        CalculateForwardKinematics();
                    }
                    else if (DetailedSelection.SelectedItem.ToString() == "XYZ")
                    {
                        CalculateInverseKinematics();
                    }
                }
                SendAllJValues();
            }

        }

        private void ManOrAuto_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ManOrAuto.SelectedItem.ToString() == "AUTO")
            {
                AutoPanel.Visible = true;
                SimpleMode.Visible = false;
                DetailedMode.Visible = false;
                SendCommand("Auto", 2);
            }
            else if (ManOrAuto.SelectedItem.ToString() == "MANUAL")
            {
                AutoPanel.Visible = false;
                SendCommand("Man", 2);
                if (comboBox1.SelectedItem.ToString() == "Simple")
                {
                    SimpleMode.Visible = true;
                    DetailedMode.Visible = false;
                }
                else if (comboBox1.SelectedItem.ToString() == "Detailed")
                {
                    SimpleMode.Visible = false;
                    DetailedMode.Visible = true;
                }
            }
        }

        private void P1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P1.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P2.Items.Remove(chosen);
                P3.Items.Remove(chosen);
                P4.Items.Remove(chosen);
                P5.Items.Remove(chosen);
                P6.Items.Remove(chosen);
            }
        }

        private void P2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P2.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P1.Items.Remove(chosen);
                P3.Items.Remove(chosen);
                P4.Items.Remove(chosen);
                P5.Items.Remove(chosen);
                P6.Items.Remove(chosen);
            }
        }

        private void P3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P3.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P1.Items.Remove(chosen);
                P2.Items.Remove(chosen);
                P4.Items.Remove(chosen);
                P5.Items.Remove(chosen);
                P6.Items.Remove(chosen);
            }
        }

        private void P4_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P4.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P1.Items.Remove(chosen);
                P2.Items.Remove(chosen);
                P3.Items.Remove(chosen);
                P5.Items.Remove(chosen);
                P6.Items.Remove(chosen);
            }
        }

        private void P5_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P5.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P1.Items.Remove(chosen);
                P2.Items.Remove(chosen);
                P3.Items.Remove(chosen);
                P4.Items.Remove(chosen);
                P6.Items.Remove(chosen);
            }
        }

        private void P6_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chosen = P6.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                P1.Items.Remove(chosen);
                P2.Items.Remove(chosen);
                P3.Items.Remove(chosen);
                P4.Items.Remove(chosen);
                P5.Items.Remove(chosen);
            }
        }


        private void Start_Click(object sender, EventArgs e)
        {
            // Danh sách các ComboBox
            System.Windows.Forms.ComboBox[] comboBoxes = { P1, P2, P3, P4, P5, P6 };

            // Kiểm tra từng ComboBox
            foreach (var comboBox in comboBoxes)
            {
                // Nếu chưa chọn hoặc chọn giá trị là " ", hiển thị thông báo lỗi
                if (comboBox.SelectedItem == null || comboBox.SelectedItem.ToString() == " ")
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ giá trị cho tất cả các mục.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            switch (P6.SelectedItem.ToString())
            {
                case "Pos1":
                    barValue(114, 57, -18, 90, 130, 110);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                case "Pos2":
                    barValue(85, 57, -18, 90, 130, 80);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                case "Pos3":
                    barValue(63, 49, 3, 83, 140, 60);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                case "PosA":
                    barValue(-38, 42, -35, 40, 14, -80);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                case "PosB":
                    barValue(-9, 42, -35, 40, 14, -80);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                case "PosC":
                    barValue(37, 40, -22, 70, 22, -78);
                    UpdateBarValue(J1TextBox, J1Bar);
                    UpdateBarValue(J2TextBox, J2Bar);
                    UpdateBarValue(J3TextBox, J3Bar);
                    UpdateBarValue(J4TextBox, J4Bar);
                    UpdateBarValue(J5TextBox, J5Bar);
                    UpdateBarValue(J6TextBox, J6Bar);
                    break;
                default:
                    MessageBox.Show("Giá trị không hợp lệ trong P6.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            var commands = new List<string> { P1.SelectedItem.ToString(), P4.SelectedItem.ToString(), P2.SelectedItem.ToString(), P5.SelectedItem.ToString(), P3.SelectedItem.ToString(), P6.SelectedItem.ToString() };
            string commandProcess = "Start/" + string.Join("/", commands);

            Console.WriteLine(commandProcess);
            SendCommand(commandProcess, 2);
        }


        private void Stop_Click(object sender, EventArgs e)
        {
            SendCommand("Pause", 2);
            // Danh sách gốc các giá trị
            List<string> choices = new List<string> { "Pos1", "Pos2", "Pos3", "PosA", "PosB", "PosC"," "};

            // Khôi phục các ComboBox P1 đến P6 về giá trị gốc
            P1.Items.Clear();
            P2.Items.Clear();
            P3.Items.Clear();
            P4.Items.Clear();
            P5.Items.Clear();
            P6.Items.Clear();

            // Thêm các mục vào các ComboBox
            P1.Items.AddRange(choices.ToArray());
            P2.Items.AddRange(choices.ToArray());
            P3.Items.AddRange(choices.ToArray());
            P4.Items.AddRange(choices.ToArray());
            P5.Items.AddRange(choices.ToArray());
            P6.Items.AddRange(choices.ToArray());

            // Đặt lại lựa chọn hiện tại nếu cần thiết (optional)
            P1.SelectedItem = " ";
            P2.SelectedItem = " ";
            P3.SelectedItem = " ";
            P4.SelectedItem = " ";
            P5.SelectedItem = " ";
            P6.SelectedItem = " ";
        }

    }
}
