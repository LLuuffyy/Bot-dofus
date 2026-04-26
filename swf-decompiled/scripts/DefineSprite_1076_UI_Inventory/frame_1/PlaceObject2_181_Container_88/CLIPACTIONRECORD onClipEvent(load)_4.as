onClipEvent(load){
   function __f_dblClick(eventObj)
   {
      _parent.dblClickItem({target:this});
   }
   this.addEventListener("dblClick",__f_dblClick);
}
