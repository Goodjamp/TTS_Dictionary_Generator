using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.Text.RegularExpressions;
using System.IO;

namespace Voice_processing
{


    public partial class Form1 : Form
    {


        FolderBrowserDialog saveFileDialog1=new FolderBrowserDialog();

        SpeechSynthesizer voice = new SpeechSynthesizer();
        static String PATHSOUND = @"D:\Privat\Sound\";
        static String WAVFOLDER = "WAV";
        static String SAMPLENANE = "sample.dic";
        static String DICNANE = "dic.dic";
        static String NO_WORD = "0,0";


        // Создаю колекцию настроечных параметров програмы
        public Dictionary<string, string> DictAlphabet = new Dictionary<string, string> {
        {"A", NO_WORD},{"B", NO_WORD},{"C", NO_WORD},{"D", NO_WORD},{"E", NO_WORD},{"F", NO_WORD},{"G", NO_WORD},
        {"H", NO_WORD},{"I", NO_WORD},{"J", NO_WORD},{"K", NO_WORD},{"L", NO_WORD},{"M", NO_WORD},{"N", NO_WORD},
        {"O", NO_WORD},{"P", NO_WORD},{"Q", NO_WORD},{"R", NO_WORD},{"S", NO_WORD},{"T", NO_WORD},{"U", NO_WORD},
        {"V", NO_WORD},{"W", NO_WORD},{"X", NO_WORD},{"Y", NO_WORD},{"Z", NO_WORD},
        };




        public Form1()
        {
            InitializeComponent();

            // Get seting voices
            foreach (InstalledVoice voice1 in voice.GetInstalledVoices())
            {
                VoiceInfo info = voice1.VoiceInfo;
                this.comboBox1.Items.Add(info.Name);
            }
            // Set default selected voice
            this.comboBox1.SelectedIndex = 1;
            // Set curen position speed
            this.textBox1.Text = this.trackBar1.Value.ToString();
            //Show curent position noise thrushold
            this.trackBar2.Value = 2;
            this.textBox2.Text = this.trackBar2.Value.ToString();
        }

        // Оттображение настройки скорости речи
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.textBox1.Text = this.trackBar1.Value.ToString();
        }


        // Оттображение настройки шумового порога
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            this.textBox2.Text = this.trackBar2.Value.ToString();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            String[] words;
            Int32 counter;
            String devider = @"\s+|\n|\d+|\.|\,|\-";
            byte[] wav_data = new byte[100000];
            byte[] byte_data = new byte[4];

            //-----------СТРОКИ ОПИСАНИЯ ОДНОГО СЛОВА ФАЙЛА БИБЛИОТЕКИ------------------------ 
            String NameWord;
            String LanthSamplWord;
            String Sampl;
            long CntRealSamples = 0;

            long last_byte = 0;
            long cnt_byte = 0;
            long cnt_entr = 0;
            long letter_shift = 0; // здвиг начала семплов слова относительно начала области семплов
            long prev_word_pos = 0; // позиция начала слов на новую букву
            int NoiseThreshold = (int)this.trackBar2.Value;
            //-----------------------------------------------------------------------------
            // однуляю словарь
            /*
            foreach (string key_ in DictAlphabet.Keys)
            {
                DictAlphabet[key_] = "Nan";
            }
            */
            // devide text to string
            words = Regex.Split(this.richTextBox1.Text, devider);
            // delate empty string
            words = words.Where(n => !string.IsNullOrEmpty(n)).ToArray();
            if (words.Length==0)
            {
                MessageBox.Show(" Введите одно или более слов", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // запускаем диалог сохранения
            this.saveFileDialog1.ShowDialog();
            PATHSOUND = this.saveFileDialog1.SelectedPath + "\\" + @"Sound\";

            //---------------------------СОЗДАЮ ДИРЕКТОРИЮ СЛОВАРЯ-------------------------------
            System.IO.Directory.CreateDirectory(PATHSOUND + "\\" + WAVFOLDER);
            //-------------------------------------------------------------------------------


            // Sort string of text 
            Array.Sort(words);
            for (counter = 0; counter < words.Length; counter++)
            {
                words[counter] = words[counter].ToUpper();  //Все буквы заглавные
            }
            words = words.Distinct().ToArray();
            // 
            voice.Rate = this.trackBar1.Value; // Удаляю повторяющиеся слова
            // 
            voice.SelectVoice(this.comboBox1.SelectedItem.ToString());
            // 
            voice.Rate = this.trackBar1.Value;
            voice.Volume = 100;
            // Configure the audio output. 

            for (counter = 0; counter < words.Length; counter++)
            {
                voice.SetOutputToWaveFile(PATHSOUND + WAVFOLDER + "\\" + words[counter] + ".wav", new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Eight, AudioChannel.Mono));
                voice.Speak(words[counter]);
                voice.SetOutputToNull();
            }

            // -----------ГЕНЕРАЦИЯ БИБЛИОТЕКИ СЕМПЛОВ/СЛОВ------------------------------------------------
            StreamWriter SampleFileStr, DicFileStr;  //Класс для записи в файл
            FileInfo SampleFile = new FileInfo(PATHSOUND + SAMPLENANE);
            SampleFileStr = SampleFile.AppendText(); //Дописываем инфу в файл, если файла не существует он создастся
            FileInfo DicFile = new FileInfo(PATHSOUND + DICNANE);
            DicFileStr = DicFile.AppendText(); //Дописываем инфу в файл, если файла не существует он создастся


            // -------------------ФОРМИРУЮ ЗАПИСЬ СЛОВАРЯ---------------------------------------------
            for (counter = 0; counter < words.Length; counter++)
            {
                //----------------КОПИРУЮ СЕМПЛЫ В СТРОКУ-----------------------------------------------
                // Считываю весь WAV файл для обработки
                FileStream samples_data = File.OpenRead(PATHSOUND + WAVFOLDER + "\\" + words[counter] + ".wav");
                Array.Clear(wav_data, 0, 10000);
                samples_data.Read(wav_data, 0, Convert.ToInt32(samples_data.Length));
                // обнуляю строку семплов
                Sampl = "";
                // Ищу первый символ из конца с которого начинаються звук               
                for (cnt_byte = samples_data.Length - 1; (wav_data[cnt_byte] > (128 - NoiseThreshold)) && (wav_data[cnt_byte] < (128 + NoiseThreshold)); cnt_byte--) { };
                last_byte = cnt_byte;
                // Ищу первый символ из начала с которого начинается звук 
                for (cnt_byte = 48; (wav_data[cnt_byte] > (128 - NoiseThreshold)) && (wav_data[cnt_byte] < (128 + NoiseThreshold)); cnt_byte++) { };
                CntRealSamples = last_byte - cnt_byte + 1;
                if (CntRealSamples < 0) {
                    MessageBox.Show("Слово "+ words[counter]+" не имеет звуковой дорожки", "Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SampleFileStr.Close(); // Закрываем файл семплов
                    DicFileStr.Close();    // Закрываем файл словаря
                    return;
                }


                // заполняю строку семплов
                for (; cnt_byte <= last_byte; cnt_byte++)
                {
                    Sampl = Sampl + Convert.ToString(wav_data[cnt_byte]) + ",";
                    // Добаляю символ переноса строки для каждого заданногоколичества символов
                    cnt_entr++;
                    if (cnt_entr >= 20) {
                        cnt_entr = 0;
                        Sampl = Sampl + "\r\n";
                    }
                }
                //-----------------------------------------------------------------------------------------
                //---------------------КОПИРУЮ РАЗМЕР ТРЕКА СЛОВА В СТРОКУ----------------------------------
                byte_data = BitConverter.GetBytes((UInt32)CntRealSamples);
                LanthSamplWord = byte_data[0].ToString() + "," + byte_data[1].ToString() + ",";
                //------------------------------------------------------------------------------------------

                // ---------------КОПИРУЮ ИМЯ ФАЙЛА ПОСИМВОЛЬНО В СТРОКУ-------------------------------
                NameWord = "";
              //  words[counter] = words[counter].ToUpper();  //Все буквы заглавные
                foreach (char name_leter in words[counter])
                    NameWord = NameWord + "'" + Convert.ToString(name_leter) + "',";//
                NameWord = NameWord + "0,";
                //--------------------------------------------------------------------------------------

                //----------------------ЗАПИСЫВАЮ НОВОЕ СЛОВО В ФАЙЛ СЛОВАРЯ--------------------------------
                SampleFileStr.Write(NameWord + LanthSamplWord + Sampl);
                //------------------------------------------------------------------------------------------
                // Заполнение словаря              
                if (counter == 0) {
                    DictAlphabet[Convert.ToString(words[counter][0])] =  letter_shift.ToString();
                    letter_shift += 3 + words[counter].Length + CntRealSamples; // позиция слова относительно начала области семплов (ЗДВИГ)
                    continue;
                }
                if (Convert.ToString(words[counter][0]) != Convert.ToString(words[counter - 1][0]))
                {
                    DictAlphabet[Convert.ToString(words[counter][0])] = letter_shift.ToString();
                    DictAlphabet[Convert.ToString(words[counter-1][0])] =DictAlphabet[Convert.ToString(words[counter-1][0])] + ',' + (counter - prev_word_pos).ToString();
                    prev_word_pos = counter;

                }
                letter_shift += 3 + words[counter].Length + CntRealSamples; // позиция слова относительно начала области семплов (ЗДВИГ)

            }
            // add last leter number words
            DictAlphabet[Convert.ToString(words[counter - 1][0])] = DictAlphabet[Convert.ToString(words[counter - 1][0])] + ',' + (counter - prev_word_pos).ToString();
            // fill dictionari file
            foreach (string my_key in DictAlphabet.Keys) {
                {
                    DicFileStr.Write("{" + '\'' + my_key + '\'' + "," + DictAlphabet[my_key] + '}' + ",\r\n");
            }

        }

             SampleFileStr.Close(); // Закрываем файл семплов
             DicFileStr.Close();    // Закрываем файл словаря
             MessageBox.Show("Added words: " + words.Length.ToString() + "\r\n" +
                            "Size of dictionary, bytes: " + letter_shift.ToString() + "\r\n" +
                            "Saved in: " + PATHSOUND,
                            "Report",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

        }


    }
}
