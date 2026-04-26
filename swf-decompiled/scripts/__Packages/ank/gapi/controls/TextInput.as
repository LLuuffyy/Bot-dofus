class ank.gapi.controls.TextInput extends ank.gapi.controls.Label
{
   var _bEnabled;
   var _tText;
   var addToQueue;
   static var CLASS_NAME = "TextInput";
   var _sTextfiledType = "input";
   var _sRestrict = "none";
   var _nMaxChars = -1;
   function TextInput()
   {
      super();
   }
   function set restrict(sRestrict)
   {
      this._sRestrict = sRestrict != "none" ? sRestrict : null;
      if(this._tText != undefined)
      {
         this.setRestrict();
      }
   }
   function get restrict()
   {
      return this._tText.restrict;
   }
   function set maxChars(nMaxChars)
   {
      this._nMaxChars = nMaxChars != -1 ? nMaxChars : null;
      if(this._tText != undefined)
      {
         this.setMaxChars();
      }
   }
   function get maxChars()
   {
      return this._tText.maxChars;
   }
   function get focused()
   {
      return eval(Selection.getFocus()) == this._tText;
   }
   function set tabIndex(nTabIndex)
   {
      this._tText.tabIndex = nTabIndex;
   }
   function get tabIndex()
   {
      return this._tText.tabIndex;
   }
   function set tabEnabled(bEnabled)
   {
      this._tText.tabEnabled = bEnabled;
   }
   function get tabEnabled()
   {
      return this._tText.tabEnabled;
   }
   function set password(bPassword)
   {
      this._tText.password = bPassword;
   }
   function get password()
   {
      return this._tText.password;
   }
   function setFocus()
   {
      if(this._tText == undefined)
      {
         this.addToQueue({object:this,method:function()
         {
            Selection.setFocus(this._tText);
         }});
      }
      else
      {
         Selection.setFocus(this._tText);
      }
   }
   function createChildren()
   {
      super.createChildren();
      this.setRestrict();
      this.setMaxChars();
   }
   function setEnabled()
   {
      if(this._bEnabled)
      {
         this._tText.type = "input";
      }
      else
      {
         this._tText.type = "dynamic";
      }
   }
   function setRestrict()
   {
      this._tText.restrict = this._sRestrict;
   }
   function setMaxChars()
   {
      this._tText.maxChars = this._nMaxChars;
   }
}
