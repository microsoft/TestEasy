<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SamplePostWebForm.aspx.cs" Inherits="TestEasyWAP.SamplePostWebForm" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <input type="file" id="fileUpload" />
        <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Button" />
    </div>
    </form>
</body>
</html>
