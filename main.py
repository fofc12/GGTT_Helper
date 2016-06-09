# -*- coding: utf-8 -*-
from PyQt4 import QtCore, QtGui
import re
"""
Module implementing maindlg.
"""

from PyQt4.QtCore import pyqtSignature
from PyQt4.QtGui import QDialog

from Ui_main import Ui_maindlg

class maindlg(QDialog, Ui_maindlg):
    """
    Class documentation goes here.
    """
    mydict={}
    input=""
    def __init__(self, parent=None):
        """
        Constructor
        
        @param parent reference to the parent widget
        @type QWidget
        """
        QDialog.__init__(self, parent)
        self.setupUi(self)
    
    def load_input(self):
        content=self.inputtxt.toPlainText()
        content=unicode(QtCore.QString(content).toUtf8(), 'utf-8', 'ignore')
        print "Query:"+content
        self.input=content
    
    def item2str(self, k, ch, pos):
        ret= ""+ch
        if(self.icodecb.isChecked()):
            ret+= (k+str(pos))
        if(self.mlinecb.isChecked()):
            ret+= "\n"
        return ret
    
    @pyqtSignature("")
    def on_loadbtn_clicked(self):
        """
        Slot documentation goes here.
        """
        # TODO: not implemented yet
        print "loading",
        file_obj = open('bm.txt')
        self.mydict={}
        i=0
        
        for line in file_obj.readlines():
             if i%10000==0:
                  print '=',
                  
             line=line.rstrip()
             uline=unicode(line,"utf-8")
             llist=line.split(" ")
             k=llist[0]
        
             llist=uline.split(" ")
             del llist[0]
             #print k,llist
             self.mydict[k]=llist
             
             i=i+1
        #print dict
        print "100%"
        self.rettxt.appendPlainText("DB loaded.")
        file_obj.close( )
        
    @pyqtSignature("")
    def on_querybtn_clicked(self):
        """
        Slot documentation goes here.
        """
        # TODO: not implemented yet
        myinput="hello"
        self.load_input()
        c=self.input
        #i=unicode(myinput,"gb2312")
        d=myinput.rstrip()
        llist=re.split(' |;|\'',d)
        #print llist
   
        i=0
        err_count=0
        #index=-1
        klist=[]
        for ch in c:
            #print ch
            sta=""
            s_ret_list=[]
            for d,x in self.mydict.items():
                  #print x
                  if ch in x:
                       #print d
                       s_ret_list.append(d+str(x.index(ch)+1))
   
            min_len_code="error!"
            for code in s_ret_list:
                  if len(code)<len(min_len_code):
                       min_len_code=code
            sta=min_len_code
             #if len(sta)!=len(llist[i]):
            if sta!=llist[i]:
                  print ch+sta+" ",
                  self.rettxt.insertPlainText(self.item2str(sta, ch, ""))
                  err_count+=1
                  
            klist.append(sta)
            i+=1
            if i>=len(llist):
                  i=0
        if err_count==0:
             print " Perfect!!!"
        else:
             print " Come on!"
        
    @pyqtSignature("")
    def on_l25btn_clicked(self):
        """
        Slot documentation goes here.
        """
        # TODO: not implemented yet
        self.load_input()
        
        #retlst=[]
        for ch in self.input:
            for k,words in self.mydict.items():
                  if ch in words:
                       if len(k)==2 and words.index(ch)+1==2:
                        #self.rettxt.moveCursor();
                        self.rettxt.insertPlainText(self.item2str(k, ch, words.index(ch)+1))
                        break
        self.rettxt.insertPlainText("\n")

    
    @pyqtSignature("")
    def on_l4btn_clicked(self):
        """
        Slot documentation goes here.
        """
        # TODO: not implemented yet
        self.load_input()
        
        for ch in self.input:
            codelst=[]
            for k,words in self.mydict.items():
                if ch in words:
                       #if len(k)==4:
                        #self.rettxt.moveCursor();
                        #self.rettxt.insertPlainText(self.item2str(k, ch, words.index(ch)+1))
                        #break
                    codelst.append(k)
                    
            min_len_code="error!"
            for code in codelst:
                if len(code)<len(min_len_code):
                       min_len_code=code
                       
            if len(min_len_code)>=4:
                #print self.item2str(k, ch, 4), 
                #print codelst, 
                self.rettxt.insertPlainText(self.item2str(min_len_code, ch, 4))
                
        print ""         
        self.rettxt.insertPlainText("\n")
        
if __name__ == "__main__":
    import sys
    app = QtGui.QApplication(sys.argv)
    ui = maindlg()
    ui.show()
    sys.exit(app.exec_())
