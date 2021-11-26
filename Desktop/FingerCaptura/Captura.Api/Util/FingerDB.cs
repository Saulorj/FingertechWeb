using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.IO;

namespace Captura.Api.DB
{
	class NdbItem
    {
		private string id;
		private string a;
		private string b;
		public NdbItem(string id, string a, string b)
        {
			this.id = id;
			this.b = b;
			this.a = a;
        }
        public string Id { get; set; }
        public string A { get; set; }
        public string B { get; set; }
    }

    public class FingerDB
    {
        private NBioAPI nBioAPI;
		private NBioAPI.NSearch m_NSearch;
		private List<NdbItem> items;

		private void ErrorMsg(string s)
        {
			Console.WriteLine(s);
		}

		private void ErrorMsg(uint ret)
		{
			Console.WriteLine(NBioAPI.Error.GetErrorDescription(ret) + " [" + ret.ToString() + "]");
		}
		public static string GetVersion(NBioAPI nBioAPI)
        {
			NBioAPI.Type.VERSION version = new NBioAPI.Type.VERSION();
			nBioAPI.GetVersion(out version);
			string Text = string.Format("NSearch C#.NET using class library (BSP Version : v{0}.{1:D4})", version.Major, version.Minor);
			return Text;
		}

		public void CloseDb()
        {
			if (m_NSearch != null)
				this.m_NSearch.ClearDB();
		}

		public FingerDB(NBioAPI nBioAPI)
        {
            this.nBioAPI = nBioAPI; 
			this.items = new List<NdbItem>(); 
			this.m_NSearch = new NBioAPI.NSearch(this.nBioAPI);
			var ret = m_NSearch.InitEngine();
			if (ret != NBioAPI.Error.NONE)
			{
				ErrorMsg(ret);
				m_NSearch = null;
			}
		}


		public string IdentificarAssociado()
        {
			NBioAPI.Type.HFIR hCapturedFIR;

			// Get FIR data
			nBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
			uint ret = nBioAPI.Capture(out hCapturedFIR);
			if (ret != NBioAPI.Error.NONE)
			{
				ErrorMsg(ret);
				nBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
				return "-1";
			}

			nBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);

			// Identify FIR to NSearch DB
			NBioAPI.NSearch.FP_INFO fpInfo;
			ret = m_NSearch.IdentifyData(hCapturedFIR, NBioAPI.Type.FIR_SECURITY_LEVEL.NORMAL, out fpInfo);
			if (ret != NBioAPI.Error.NONE)
			{
				ErrorMsg(ret);
				return "-1";
			}


			return fpInfo.ID.ToString();
		}

		public bool SaveDB(string fileName)
        {
			uint ret = NBioAPI.Error.NONE;

			if (m_NSearch == null)
			{
				ErrorMsg("Não foi possível inicializar o Engine de busca para gravação");
				return false;
			}
			string szFileName = "";
			//saveFileDialog1.Filter = "FDB files (*.fdb)|*.fdb";

			szFileName = fileName;
			if (szFileName != "")
			{
				// Save NSearchDB to File
				ret = m_NSearch.SaveDBToFile(szFileName);
				if (ret != NBioAPI.Error.NONE)
				{
					ErrorMsg(ret);
					return false;
				}

				// Save list to file
				szFileName = Path.ChangeExtension(szFileName, "FID");

				FileStream fs = new FileStream(szFileName, FileMode.OpenOrCreate, FileAccess.Write);
				StreamWriter fw = new StreamWriter(fs);

				foreach (NdbItem item in items)	
				{
					fw.WriteLine(item.Id + "\t" + item.A + "\t" + item.B);
				}

				fw.Close();
				fs.Close();

				return true;
			}
			else
			{
				return false;
			}
		}

        public bool LoadDB(string fileName) 
        {
			uint ret = NBioAPI.Error.NONE;

			// Clear NSearchDB
			m_NSearch.ClearDB();

			var szFileName = fileName;
			if (szFileName != "")
			{
				// Load NSearchDB from File
				ret = m_NSearch.LoadDBFromFile(szFileName);
				if (ret != NBioAPI.Error.NONE)
				{
					ErrorMsg(ret);
					return false;
				}

				// Load list from file
				szFileName = Path.ChangeExtension(szFileName, "FID");

				FileStream fs = new FileStream(szFileName, FileMode.Open, FileAccess.Read);
				StreamReader fr = new StreamReader(fs);

				while (fr.Peek() >= 0)
				{
					try
					{
						string szLine = fr.ReadLine();
						string[] szSplit = szLine.Split('\t');
						var item = new NdbItem(szSplit[0], szSplit[1], szSplit[2]);
						items.Add(item);
					}
					catch
					{
						break;
					}
				}

				fr.Close();
				fs.Close();
				return true;
			}
			else
			{
				ErrorMsg("Nome de arquivo inválido");
				return false;
			}
		}
    }
}
