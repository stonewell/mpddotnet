#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed val the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace Mule.Preference.Impl
{
    [System.Xml.Serialization.XmlRoot("ProxySettings")]
    class ProxySettingsImpl : ProxySettings
    {
        private ushort Type_;
        public ushort Type
        {
            get { return Type_; }
            set { Type_ = value; }
        }

        private ushort Port_;
        public ushort Port
        {
            get { return Port_; }
            set { Port_ = value; }
        }

        private string Name_;
        public string Name
        {
            get { return Name_; }
            set { Name_ = value; }
        }

        private string User_;
        public string User
        {
            get { return User_; }
            set { User_ = value; }
        }

        private string Password_;
        public string Password
        {
            get { return Password_; }
            set { Password_ = value; }
        }

        private bool IsPasswordEnabled_;
        public bool IsPasswordEnabled
        {
            get { return IsPasswordEnabled_; }
            set { IsPasswordEnabled_ = value; }
        }

        private bool UseProxy_;
        public bool UseProxy
        {
            get { return UseProxy_; }
            set { UseProxy_ = value; }
        }


        #region ProxySettings Members
        private static readonly System.Xml.Serialization.XmlSerializer xs_ =
            new System.Xml.Serialization.XmlSerializer(typeof(ProxySettingsImpl));

        public void LoadFrom(System.IO.MemoryStream ms)
        {
            ProxySettingsImpl tmp =
                xs_.Deserialize(ms) as ProxySettingsImpl;

            this.IsPasswordEnabled = tmp.IsPasswordEnabled;
            this.Name = tmp.Name;
            this.Password = tmp.Password;
            this.Port = tmp.Port;
            this.Type = tmp.Type;
            this.UseProxy = tmp.UseProxy;
            this.User = tmp.User;
        }

        public void SaveTo(System.IO.MemoryStream ms)
        {
            xs_.Serialize(ms, this);
        }

        #endregion
    }
}
