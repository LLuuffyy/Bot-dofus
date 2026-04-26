class dofus.graphics.gapi.controls.temporis.TemporisRules extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _taRules;
   var addToQueue;
   var api;
   static var CLASS_NAME = "TemporisRules";
   function TemporisRules()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.temporis.TemporisRules.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
   }
   function addListeners()
   {
   }
   function initData()
   {
      this._taRules.text = this.api.lang.getText("TEMPORIS_RULES_LONG");
      this._taRules.filters = [new flash.filters.GlowFilter(2500134,1,3,3,1,1)];
   }
}
