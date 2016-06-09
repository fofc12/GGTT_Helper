# -*- coding: utf-8 -*-

# Form implementation generated from reading ui file 'D:\workspace\ggtt_helper\main.ui'
#
# Created by: PyQt4 UI code generator 4.11.4
#
# WARNING! All changes made in this file will be lost!

from PyQt4 import QtCore, QtGui

try:
    _fromUtf8 = QtCore.QString.fromUtf8
except AttributeError:
    def _fromUtf8(s):
        return s

try:
    _encoding = QtGui.QApplication.UnicodeUTF8
    def _translate(context, text, disambig):
        return QtGui.QApplication.translate(context, text, disambig, _encoding)
except AttributeError:
    def _translate(context, text, disambig):
        return QtGui.QApplication.translate(context, text, disambig)

class Ui_maindlg(object):
    def setupUi(self, maindlg):
        maindlg.setObjectName(_fromUtf8("maindlg"))
        maindlg.resize(309, 444)
        maindlg.setSizeGripEnabled(True)
        self.loadbtn = QtGui.QPushButton(maindlg)
        self.loadbtn.setGeometry(QtCore.QRect(10, 20, 75, 23))
        self.loadbtn.setObjectName(_fromUtf8("loadbtn"))
        self.rettxt = QtGui.QPlainTextEdit(maindlg)
        self.rettxt.setGeometry(QtCore.QRect(10, 290, 281, 131))
        self.rettxt.setObjectName(_fromUtf8("rettxt"))
        self.querybtn = QtGui.QPushButton(maindlg)
        self.querybtn.setGeometry(QtCore.QRect(10, 170, 75, 23))
        self.querybtn.setObjectName(_fromUtf8("querybtn"))
        self.inputtxt = QtGui.QPlainTextEdit(maindlg)
        self.inputtxt.setGeometry(QtCore.QRect(10, 50, 281, 111))
        self.inputtxt.setObjectName(_fromUtf8("inputtxt"))
        self.l25btn = QtGui.QPushButton(maindlg)
        self.l25btn.setGeometry(QtCore.QRect(110, 170, 75, 23))
        self.l25btn.setObjectName(_fromUtf8("l25btn"))
        self.mlinecb = QtGui.QCheckBox(maindlg)
        self.mlinecb.setGeometry(QtCore.QRect(220, 260, 71, 16))
        self.mlinecb.setObjectName(_fromUtf8("mlinecb"))
        self.icodecb = QtGui.QCheckBox(maindlg)
        self.icodecb.setGeometry(QtCore.QRect(10, 260, 101, 16))
        self.icodecb.setObjectName(_fromUtf8("icodecb"))
        self.l4btn = QtGui.QPushButton(maindlg)
        self.l4btn.setGeometry(QtCore.QRect(210, 170, 75, 23))
        self.l4btn.setObjectName(_fromUtf8("l4btn"))

        self.retranslateUi(maindlg)
        QtCore.QMetaObject.connectSlotsByName(maindlg)

    def retranslateUi(self, maindlg):
        maindlg.setWindowTitle(_translate("maindlg", "Dialog", None))
        self.loadbtn.setText(_translate("maindlg", "载入词库", None))
        self.rettxt.setPlainText(_translate("maindlg", "欢迎使用五笔助手测试版 v0.1 By 零西", None))
        self.querybtn.setText(_translate("maindlg", "查询编码", None))
        self.inputtxt.setPlainText(_translate("maindlg", "欢迎使用五笔助手", None))
        self.l25btn.setText(_translate("maindlg", "次二级简码", None))
        self.mlinecb.setText(_translate("maindlg", "CheckBox", None))
        self.icodecb.setText(_translate("maindlg", "输出时含码", None))
        self.l4btn.setText(_translate("maindlg", "四码字", None))


if __name__ == "__main__":
    import sys
    app = QtGui.QApplication(sys.argv)
    maindlg = QtGui.QDialog()
    ui = Ui_maindlg()
    ui.setupUi(maindlg)
    maindlg.show()
    sys.exit(app.exec_())

