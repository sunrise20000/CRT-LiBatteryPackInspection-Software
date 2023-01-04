using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aitex.Core.RT.Key
{
	public class RsaCryption
	{
		public void RSAKey(out string xmlKeys, out string xmlPublicKey)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				xmlKeys = rSACryptoServiceProvider.ToXmlString(includePrivateParameters: true);
				xmlPublicKey = rSACryptoServiceProvider.ToXmlString(includePrivateParameters: false);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public string RSAEncrypt(string xmlPublicKey, string encryptString)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(xmlPublicKey);
				byte[] bytes = new UnicodeEncoding().GetBytes(encryptString);
				byte[] inArray = rSACryptoServiceProvider.Encrypt(bytes, fOAEP: false);
				return Convert.ToBase64String(inArray);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public string RSAEncrypt(string xmlPublicKey, byte[] EncryptString)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(xmlPublicKey);
				byte[] inArray = rSACryptoServiceProvider.Encrypt(EncryptString, fOAEP: false);
				return Convert.ToBase64String(inArray);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public string RSADecrypt(string xmlPrivateKey, string decryptString)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(xmlPrivateKey);
				byte[] rgb = Convert.FromBase64String(decryptString);
				byte[] bytes = rSACryptoServiceProvider.Decrypt(rgb, fOAEP: false);
				return new UnicodeEncoding().GetString(bytes);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public string RSADecrypt(string xmlPrivateKey, byte[] DecryptString)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(xmlPrivateKey);
				byte[] bytes = rSACryptoServiceProvider.Decrypt(DecryptString, fOAEP: false);
				return new UnicodeEncoding().GetString(bytes);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool GetHash(string strSource, ref byte[] HashData)
		{
			try
			{
				HashAlgorithm hashAlgorithm = HashAlgorithm.Create("MD5");
				byte[] bytes = Encoding.GetEncoding("GB2312").GetBytes(strSource);
				HashData = hashAlgorithm.ComputeHash(bytes);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool GetHash(string strSource, ref string strHashData)
		{
			try
			{
				HashAlgorithm hashAlgorithm = HashAlgorithm.Create("MD5");
				byte[] bytes = Encoding.GetEncoding("GB2312").GetBytes(strSource);
				byte[] inArray = hashAlgorithm.ComputeHash(bytes);
				strHashData = Convert.ToBase64String(inArray);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool GetHash(FileStream objFile, ref byte[] HashData)
		{
			try
			{
				HashAlgorithm hashAlgorithm = HashAlgorithm.Create("MD5");
				HashData = hashAlgorithm.ComputeHash(objFile);
				objFile.Close();
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool GetHash(FileStream objFile, ref string strHashData)
		{
			try
			{
				HashAlgorithm hashAlgorithm = HashAlgorithm.Create("MD5");
				byte[] inArray = hashAlgorithm.ComputeHash(objFile);
				objFile.Close();
				strHashData = Convert.ToBase64String(inArray);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureFormatter(string strKeyPrivate, byte[] HashbyteSignature, ref byte[] EncryptedSignatureData)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPrivate);
				RSAPKCS1SignatureFormatter rSAPKCS1SignatureFormatter = new RSAPKCS1SignatureFormatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureFormatter.SetHashAlgorithm("MD5");
				EncryptedSignatureData = rSAPKCS1SignatureFormatter.CreateSignature(HashbyteSignature);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureFormatter(string strKeyPrivate, byte[] HashbyteSignature, ref string strEncryptedSignatureData)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPrivate);
				RSAPKCS1SignatureFormatter rSAPKCS1SignatureFormatter = new RSAPKCS1SignatureFormatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureFormatter.SetHashAlgorithm("MD5");
				byte[] inArray = rSAPKCS1SignatureFormatter.CreateSignature(HashbyteSignature);
				strEncryptedSignatureData = Convert.ToBase64String(inArray);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureFormatter(string strKeyPrivate, string strHashbyteSignature, ref byte[] EncryptedSignatureData)
		{
			try
			{
				byte[] rgbHash = Convert.FromBase64String(strHashbyteSignature);
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPrivate);
				RSAPKCS1SignatureFormatter rSAPKCS1SignatureFormatter = new RSAPKCS1SignatureFormatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureFormatter.SetHashAlgorithm("MD5");
				EncryptedSignatureData = rSAPKCS1SignatureFormatter.CreateSignature(rgbHash);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureFormatter(string strKeyPrivate, string strHashbyteSignature, ref string strEncryptedSignatureData)
		{
			try
			{
				byte[] rgbHash = Convert.FromBase64String(strHashbyteSignature);
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPrivate);
				RSAPKCS1SignatureFormatter rSAPKCS1SignatureFormatter = new RSAPKCS1SignatureFormatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureFormatter.SetHashAlgorithm("MD5");
				byte[] inArray = rSAPKCS1SignatureFormatter.CreateSignature(rgbHash);
				strEncryptedSignatureData = Convert.ToBase64String(inArray);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureDeformatter(string strKeyPublic, byte[] HashbyteDeformatter, byte[] DeformatterData)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPublic);
				RSAPKCS1SignatureDeformatter rSAPKCS1SignatureDeformatter = new RSAPKCS1SignatureDeformatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureDeformatter.SetHashAlgorithm("MD5");
				if (rSAPKCS1SignatureDeformatter.VerifySignature(HashbyteDeformatter, DeformatterData))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureDeformatter(string strKeyPublic, string strHashbyteDeformatter, byte[] DeformatterData)
		{
			try
			{
				byte[] rgbHash = Convert.FromBase64String(strHashbyteDeformatter);
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPublic);
				RSAPKCS1SignatureDeformatter rSAPKCS1SignatureDeformatter = new RSAPKCS1SignatureDeformatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureDeformatter.SetHashAlgorithm("MD5");
				if (rSAPKCS1SignatureDeformatter.VerifySignature(rgbHash, DeformatterData))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureDeformatter(string strKeyPublic, byte[] HashbyteDeformatter, string strDeformatterData)
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPublic);
				RSAPKCS1SignatureDeformatter rSAPKCS1SignatureDeformatter = new RSAPKCS1SignatureDeformatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureDeformatter.SetHashAlgorithm("MD5");
				byte[] rgbSignature = Convert.FromBase64String(strDeformatterData);
				if (rSAPKCS1SignatureDeformatter.VerifySignature(HashbyteDeformatter, rgbSignature))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SignatureDeformatter(string strKeyPublic, string strHashbyteDeformatter, string strDeformatterData)
		{
			try
			{
				byte[] rgbHash = Convert.FromBase64String(strHashbyteDeformatter);
				RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.FromXmlString(strKeyPublic);
				RSAPKCS1SignatureDeformatter rSAPKCS1SignatureDeformatter = new RSAPKCS1SignatureDeformatter(rSACryptoServiceProvider);
				rSAPKCS1SignatureDeformatter.SetHashAlgorithm("MD5");
				byte[] rgbSignature = Convert.FromBase64String(strDeformatterData);
				if (rSAPKCS1SignatureDeformatter.VerifySignature(rgbHash, rgbSignature))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}
