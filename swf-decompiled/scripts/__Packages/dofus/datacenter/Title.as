class dofus.datacenter.Title
{
   var _color;
   var _id;
   var _selected;
   var _text;
   var _value;
   var api;
   function Title(id, color, value, selected, param)
   {
      this.api = _global.API;
      this._id = id;
      this._selected = selected;
      this._value = value;
      var _loc8_;
      if(value != "")
      {
         this._color = color;
         this._value = value;
      }
      else if(this._id != -1)
      {
         switch(this.api.lang.getTitle(id).pt)
         {
            case 1:
               _loc8_ = this.api.lang.getTitle(id).t.split("%1").join(this.api.lang.getMonsters()[_global.parseInt(param)].n);
               break;
            case 0:
            default:
               _loc8_ = this.api.lang.getTitle(id).t.split("%1").join(param);
         }
         this._color = color != undefined ? _global.parseInt(color) : this.api.lang.getTitle(id).c;
         this._text = "« " + _loc8_ + " »";
      }
      else
      {
         this._color = _global.parseInt(color);
         this._text = param;
      }
   }
   function get color()
   {
      return this._color;
   }
   function set color(sColor)
   {
      this._color = sColor;
   }
   function get text()
   {
      return this._value != "" ? this._value : this._text;
   }
   function set text(sText)
   {
      this._text = sText;
   }
   function get value()
   {
      return this._value;
   }
   function set value(sValue)
   {
      this._value = sValue;
   }
   function get id()
   {
      return Number(this._id);
   }
   function set id(sId)
   {
      this._id = sId;
   }
   function get selected()
   {
      return this._selected;
   }
   function set selected(sSelected)
   {
      this._selected = sSelected;
   }
}
