function scrollToLastMessage()
{
    if (document.getElementById('chatmessagesdiv')) {
        var elem = document.getElementById('chatmessagesdiv');
        elem.scrollTop = elem.scrollHeight;
        return true;
    }
    return false;
}