﻿using System.Xml;
using Aitex.Core.RT.SCCore;

namespace SicPM.Devices
{
    public class SicAds : TcAds
    {
        //public override string Address
        //{
        //    get
        //    {
        //        return SC.GetStringValue("PM1.AdsIPAddr");
        //    }
        //}

        public SicAds(string module, XmlElement node, string ioModule = "") : base(module, node, ioModule)
        {
        }
    }
}
