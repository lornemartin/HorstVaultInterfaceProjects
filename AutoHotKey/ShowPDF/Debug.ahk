#SingleInstance,force
#InstallKeybdHook
;#NoTrayIcon
intro:="
(
The script is written by nepter, administrator of <a href=""http://bbs.ahk8.com"">Chinese autohotkey forum</a>.

Function: Press a hotkey, Capslock as default, to retrieve text under cursor and show it. It could get text in gui, html, instant messager and etc. The original Capslock is replaced by Shift+Capslock.  A tray icon in notification area could change the hotkey.

You have to run it above WinXp sp3 and by Autohotkey_L 

Version: ScreenReader 0.1.1a
)"
menu,tray,NoStandard
menu,tray,add,Hotkey,hotkey
menu,tray,add,About...,about
menu,tray,add,Exit,exit
CoordMode,mouse,screen
KeyName:="f4"
Hotkey,%KeyName%,main
if !uia:=ComObjCreate("{ff48dba4-60ef-4201-aa87-54103eef594e}","{30cbe57d-d9d0-452a-ab13-7ac5ac4825ee}"){
	msgbox UI Automation Failed.
	ExitApp
}
return
+f4::f4
exit:
	exitapp
hotkey:
	gui,1:Destroy
	gui,1:add,text,,New Hotkey
	gui,1:add,Hotkey,vChosenHotkey,%KeyName%
	gui,1:add,button,Default gbtnHK,Confirm
	gui,1:show,,%A_Space%
	return
about:
	gui,2:Destroy
	gui,2:add,link,,%intro%
	gui,2:show,,About ScreenReader 0.1.1a
	return
btnHK:
	gui,1:submit
	if (ChosenHotkey!=KeyName){
		Hotkey,%KeyName%,,off
		KeyName:=ChosenHotkey
		Hotkey,%KeyName%,main,on
	}
	gui,1:Destroy
	return
main:
MouseGetPos,x,y
;DllCall(vt)
item:=GetElementItem(x,y)


Gui, Submit ; This takes the user's input from above.
	msg := item.2
	MsgBox, Value Is: %msg%


;index := 0
;Loop 10
;{
;	Gui, Submit ; This takes the user's input from above.
;	msg := item.index
;	MsgBox, Value Is: %msg%
;	index += 1
;}
	
path = M:\PDF Drawing Files\
vaultName := item.2
;SplitPath, vaultName, name, dir, ext, name_no_ext, drive ; this was acting up with filenames with dots in them
;pdfName := name_no_ext

IfInString, vaultName, .ipt
{
	StringReplace, pdfName, vaultName, .ipt,
}

else IfInString, vaultName, .iam
{
	StringReplace, pdfName, vaultName, .iam,
}

else 
{
	pdfName = %vaultName%
}

IfExist %path%%pdfName%.pdf
{
	Run, %path%%pdfName%.pdf

	Sleep 1000
	GetKeyState, state, F4
	if state = U
		WinClose A	;close the active window
}


;Loop
;{
;     MouseGetPos,x1,y1
;     Sleep,500
;     MouseGetPos,x2,y2
;     If ((x1<>x2) or (y1<>y2))             ;-- Checking to see if the mouse has moved.
;     {
		; if key stays press, leave window open, otherwise close it
;		GetKeyState, state, F4 ;  D if F4 is ON or U otherwise.
;		if state = U
;			WinClose Form1
;		break
;	 }
;}

;gui,3:Destroy
;;gui,3:new,ToolWindow
;for k,v in item
;{
;	gui,3:add,edit,x5 w480 -Tabstop vedit%k%,%v%
;	gui,3:add,button,X+5 yp-2 vbtn%k% gcp2cb,To Clipboard
;}
;	gui,3:show,,You Get
;return
vas(obj,ByRef txt){
	for k,v in obj
		if (v=txt)
			return 0
	return 1
}
cp2cb:
	n:=SubStr(A_GuiControl,4)
	GuiControlGet,txt,,edit%n%
	if txt
		Clipboard:=txt
	gui,3:Destroy
return
3GuiEscape:
	gui,3:Destroy
	return
GetPatternName(id){
	global uia
	DllCall(vt(uia,50),"ptr",uia,"uint",id,"ptr*",name)
	return StrGet(name)
}
GetPropertyName(id){
	global uia
	DllCall(vt(uia,49),"ptr",uia,"uint",id,"ptr*",name)
	return StrGet(name)
}
GetElementItem(x,y){
	global uia
	item:={}
	DllCall(vt(uia,7),"ptr",uia,"int64",x|y<<32,"ptr*",element) ;IUIAutomation::ElementFromPoint
        if !element
            return
	DllCall(vt(element,23),"ptr",element,"ptr*",name) ;IUIAutomationElement::CurrentName
	DllCall(vt(element,10),"ptr",element,"uint",30045,"ptr",variant(val)) ;IUIAutomationElement::GetCurrentPropertyValue::value
	DllCall(vt(element,10),"ptr",element,"uint",30092,"ptr",variant(lname)) ;IUIAutomationElement::GetCurrentPropertyValue::lname
	DllCall(vt(element,10),"ptr",element,"uint",30093,"ptr",variant(lval)) ;IUIAutomationElement::GetCurrentPropertyValue::lvalue
	a:=StrGet(name,"utf-16"),b:=StrGet(NumGet(val,8,"ptr"),"utf-16"),c:=StrGet(NumGet(lname,8,"ptr"),"utf-16"),d:=StrGet(NumGet(lval,8,"ptr"),"utf-16")
	a?item.Insert(a):0
	b&&vas(item,b)?item.Insert(b):0
	c&&vas(item,c)?item.Insert(c):0
	d&&vas(item,d)?item.Insert(d):0
	DllCall(vt(element,21),"ptr",element,"uint*",type) ;IUIAutomationElement::CurrentControlType
	if (type=50004)
		e:=GetElementWhole(element),e&&vas(item,e)?item.Insert(e):0
	ObjRelease(element)
	return item
}
GetElementWhole(element){
	global uia
	static init:=1,trueCondition,walker
	if init
		init:=DllCall(vt(uia,21),"ptr",uia,"ptr*",trueCondition) ;IUIAutomation::CreateTrueCondition
		,init+=DllCall(vt(uia,14),"ptr",uia,"ptr*",walker) ;IUIAutomation::ControlViewWalker
	DllCall(vt(uia,5),"ptr",uia,"ptr*",root) ;IUIAutomation::GetRootElement
	DllCall(vt(uia,3),"ptr",uia,"ptr",element,"ptr",root,"int*",same) ;IUIAutomation::CompareElements
	ObjRelease(root)
	if same {
		return
	}
	hr:=DllCall(vt(walker,3),"ptr",walker,"ptr",element,"ptr*",parent) ;IUIAutomationTreeWalker::GetParentElement
	if parent {
		e:=""
		DllCall(vt(parent,6),"ptr",parent,"uint",2,"ptr",trueCondition,"ptr*",array) ;IUIAutomationElement::FindAll
		DllCall(vt(array,3),"ptr",array,"int*",length) ;IUIAutomationElementArray::Length
		loop % length {
			DllCall(vt(array,4),"ptr",array,"int",A_Index-1,"ptr*",newElement) ;IUIAutomationElementArray::GetElement
			DllCall(vt(newElement,23),"ptr",newElement,"ptr*",name) ;IUIAutomationElement::CurrentName
			e.=StrGet(name,"utf-16")
			ObjRelease(newElement)
		}
                ObjRelease(array)
		ObjRelease(parent)
		return e
	}
}
variant(ByRef var,type=0,val=0){
	return (VarSetCapacity(var,8+2*A_PtrSize)+NumPut(type,var,0,"short")+NumPut(val,var,8,"ptr"))*0+&var
}
vt(p,n){
	return NumGet(NumGet(p+0,"ptr")+n*A_PtrSize,"ptr")
}