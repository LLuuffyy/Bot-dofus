onClipEvent(load){
   function __f_click(eventObj)
   {
      _parent.selectItem({target:this});
   }
   this.addEventListener("click",__f_click);
}
