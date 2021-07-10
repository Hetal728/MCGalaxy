/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.DB;

namespace MCGalaxy.Eco {
    
    public sealed class LoginMessageItem : SimpleItem {
        
        public LoginMessageItem() {
            Aliases = new string[] { "login", "loginmsg", "loginmessage" };
        }
        
        public override string Name { get { return "LoginMessage"; } }
        
        protected internal override void OnPurchase(Player p, string msg) {
            if (msg.Length == 0) {
                PlayerDB.SetLoginMessage(p.name, "");
                p.Message("&aYour login message was removed for free.");
                return;
            }
            
            if (p.muted) {
                p.Message("%WYou cannot purchase this while you are muted.");
                return;
            }

        	if (!CheckPrice(p)) return;
            if (msg == PlayerDB.GetLoginMessage(p)) {
                p.Message("&WYou already have that login message."); return;
            }
            if (msg.Length > NetUtils.StringSize) {
                p.Message("&WLogin message must be 64 characters or less."); return;
            }
            
            UseCommand(p, "LoginMessage", "-own " + msg);
            Economy.MakePurchase(p, Price, "%3LoginMessage: %f" + msg);
        }
    }
    
    public sealed class LogoutMessageItem : SimpleItem {
        
        public LogoutMessageItem() {
            Aliases = new string[] { "logout", "logoutmsg", "logoutmessage" };
        }
        
        public override string Name { get { return "LogoutMessage"; } }

        protected internal override void OnPurchase(Player p, string msg) {
            if (msg.Length == 0) {
                PlayerDB.SetLogoutMessage(p.name, "");
                p.Message("&aYour logout message was removed for free.");
                return;
        	}
            
            if (p.muted) {
                p.Message("%WYou cannot purchase this while you are muted.");
                return;
            }
            
        	if (!CheckPrice(p)) return;    
            if (msg == PlayerDB.GetLogoutMessage(p)) {
                p.Message("&WYou already have that logout message."); return;
            }       
            if (msg.Length > NetUtils.StringSize) {
                p.Message("&WLogin message must be 64 characters or less."); return;
            }
            
            UseCommand(p, "LogoutMessage", "-own " + msg);
            Economy.MakePurchase(p, Price, "%3LogoutMessage: %f" + msg);
        }
    }
}
