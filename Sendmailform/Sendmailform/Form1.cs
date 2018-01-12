using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.IO;
namespace Sendmailform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBoxPET.Text = "Enter Key For Encryption";
        }
        static string password = "";
        static string plaintext ="";
        static string encryptedstring = "";
        static string[] a = new string[]{"smtp.gmail.com,587","smtp.mail.yahoo.com,25","smtp.live.com,587",
                                  "smtp.live.com,25","smtp.mail.com,587","smtp.rediffmail.com,25","Enter smtp server,Enter Port"};
        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                SmtpClient client = new SmtpClient(textBoxSMTP.Text, int.Parse(textBoxPORT.Text));
                client.EnableSsl = true;
                client.Timeout = 100000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(textBoxFrom.Text, textBoxPassword.Text);
                MailMessage msg = new MailMessage();
                msg.To.Add(textBoxTo.Text);
                msg.From = new MailAddress(textBoxFrom.Text);
                msg.Subject = textBoxSubject.Text;
                foreach (int x in checkedListBox1.CheckedIndices)
                {
                    if (x == 0)
                    {
                        msg.Body = textBoxET.Text;
                        if (textBoxEF.Text != "")
                        {
                            Attachment data = new Attachment(textBoxEF.Text);
                            msg.Attachments.Add(data);
                        }
                    }
                    else
                    {
                        msg.Body = textBoxMsg.Text;

                        if (textBoxAtt.Text != "")
                        {
                            Attachment data = new Attachment(textBoxAtt.Text);
                            msg.Attachments.Add(data);
                        }
                    }
                }
                client.Send(msg);
                MessageBox.Show("Successfully Sent Message!!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + "login your Gmail and then go to this Link and select Turn On option->"
                    + "https://www.google.com/settings/security/lesssecureapps");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] str = a[comboBox1.SelectedIndex].Split(',');
            textBoxSMTP.Text = str[0];
            textBoxPORT.Text = str[1];
        }





        public static class StringCipher
        {
            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt(string plainText, string passPhrase)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }




        private void EncryptFile(string inputFile, string outputFile)
        {

            try
            {
                string password = textBoxPET.Text; // Your Key Here
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                string cryptFile = outputFile;
                FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateEncryptor(key, key),
                    CryptoStreamMode.Write);

                FileStream fsIn = new FileStream(inputFile, FileMode.Open);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);


                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch
            {
                MessageBox.Show("Encryption failed!", "Error");
            }
        }



        private void buttonET_Click(object sender, EventArgs e)
        {
            password = textBoxPET.Text;
            plaintext = textBoxMsg.Text;
            encryptedstring = StringCipher.Encrypt(plaintext, password);
            textBoxET.Text = encryptedstring;
            string str = textBoxAtt.Text;
            if (str != "")
            {
                string ef = str + "." + "jsg";
                if (!File.Exists(ef))
                {
                    EncryptFile(str, ef);
                    textBoxEF.Text = ef;
                }
                else if (File.Exists(ef))
                {
                    textBoxEF.Text = ef;
                }
            }
        }



        private void textBoxSMTP_Click(object sender, EventArgs e)
        {
            textBoxSMTP.Text = "";
        }

        private void textBoxPORT_Click(object sender, EventArgs e)
        {
            textBoxPORT.Text = "";
        }

        private void textBoxPET_Click(object sender, EventArgs e)
        {
            textBoxPET.Text = "";
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            for (int ix = 0; ix < checkedListBox1.Items.Count; ++ix)
                if (ix != e.Index) checkedListBox1.SetItemChecked(ix, false);
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void textBoxPET_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxET_TextChanged(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form0 obj = new Form0();
            obj.Show();
            this.Hide();
        }

        private void buttonAtt_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog()==DialogResult.OK)
            {
                textBoxAtt.Text = dlg.FileName.ToString();
            }
        }
    }
 
}
