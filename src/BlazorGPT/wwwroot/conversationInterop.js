
export function showPrompt(msg) {
    prompt(msg);
}

export function scrollToBottom(elementId) {
    if (elementId == null) {
        window.scrollTo(0, document.body.scrollHeight);
        return;
    }
    var objDiv = document.getElementById(elementId);
    objDiv.scrollTop = objDiv.scrollHeight;
}


export function focusElement(element) {
    console.log("focusing " + element.id);
    element.focus({ focusVisible: true });

}
export function blurElement(element) {
    element.blur();
}



export function preventDefaultOnEnter(element, remove = false) {
    var preventDefaultOnEnterFunction = function (e) {
        if (e.keyCode === 13 || e.key === "Enter") {
            e.preventDefault()
            return false
        }
    }
    if (remove) {
        element.removeEventListener('keydown', preventDefaultOnEnterFunction, false);
    }
    else {
        element.addEventListener('keydown', preventDefaultOnEnterFunction, false);
    }
}

export function setupCopyButtons() {

    if (typeof ClipboardJS === 'undefined') {
        // ClipboardJS is not defined, do something here like loading the library
    } else {
        new ClipboardJS('.copyBtn', {
            target: function (trigger) {
                return trigger.nextElementSibling;
            }
        });
    }


}

export function openWindow(url) {
    let params = ``;
    var win = pen(url, '', params);
    console.log("opened window " + win.name);
    return win.name;
}

// make an array of window references, I need to get each reference by it's name property
var windows = [];
function pen(url, name, params) {
    console.log("opening window " + name);
    var win = window.open(url, name, params);
    windows.push(win);
    return win;
}
export function openStateViewer(stateType, stateId, renderType) {
    let params = `scrollbars=no,resizable=no,status=no,location=no,toolbar=no,menubar=no,
width=1000,height=800,left=1000,top=100`;
    var win = pen('/state/' + stateType + '/' + renderType + '/' + stateId, stateType + stateId, params);
    console.log("opened window " + win.name);
    return win.name;
}
export function closeStateViewer(stateType, stateId) {
    var winName = stateType + stateId;
    var win = windows.find(x => x.name == winName);
    if (win != null) {
        win.close();
    }
}
export function getWindows() {
    return windows;
}
export function getWindowByName(name) {
    var win = windows.find(x => x.name == name);
    return win;
}
export function getWindowByStateId(stateType, stateId) {
    var winName = stateType + stateId;
    var win = windows.find(x => x.name == winName);
    return win;
}
export function getWindowByStateType(stateType) {
    var win = windows.find(x => x.name.startsWith(stateType));
    return win;
}
export function getWindowByStateTypeAndId(stateType, stateId) {
    var winName = stateType + stateId;
    var win = windows.find(x => x.name.startsWith(stateType) && x.name == winName);
    return win;
}
export function getWindowByStateTypeAndSubId(stateType, stateSubId) {
    var win = windows.find(x => x.name.startsWith(stateType) && x.name.endsWith(stateSubId));
    return win;
}
export function getWindowByStateTypeAndSubIdAndId(stateType, stateSubId, stateId) {
    var winName = stateType + stateId
}

export function setupFileArea() {
    document.getElementById('upload-button').addEventListener('click', function () {
        document.getElementById('file-input').click();
    });
}