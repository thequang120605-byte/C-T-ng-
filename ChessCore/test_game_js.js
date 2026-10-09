const fs = require('fs');
const { JSDOM } = require('jsdom');

const html = 
<!DOCTYPE html>
<html>
<head></head>
<body>
    <div id="mode-selection"></div>
    <div id="game-content"></div>
    <div id="player-vs-player-btn"></div>
    <div id="player-vs-computer-btn"></div>
    <div id="player-vs-online-btn"></div>
    <div id="online-selection"></div>
    <div id="join-online-btn"></div>
    <input id="online-room-id" />
    <div id="start-game-btn"></div>
    <div id="selected-mode-text"></div>
    <div id="difficulty-selection"></div>
    <div class="difficulty-option"></div>
    <div id="difficulty-description"></div>
    <div id="selected-difficulty-container"></div>
    <div id="selected-difficulty-text"></div>
    <div id="change-mode-btn"></div>
    <div id="pieces-layer"></div>
    <div id="moves-layer"></div>
    <div id="new-game-btn"></div>
    <div id="resign-btn"></div>
    <div id="result-new-game-btn"></div>
    <div id="result-close-btn"></div>
    <div id="confirm-resign-btn"></div>
    <div id="cancel-resign-btn"></div>
    <div id="game-result-modal"></div>
    <div id="resign-modal"></div>
</body>
</html>
;

const dom = new JSDOM(html, { runScripts: "dangerously" });
const window = dom.window;
const document = window.document;

// Mock signalR
window.signalR = {
    HubConnectionBuilder: class {
        withUrl() { return this; }
        withAutomaticReconnect() { return this; }
        build() {
            return {
                on: () => {},
                start: () => Promise.resolve()
            };
        }
    }
};

const scriptCode = fs.readFileSync('wwwroot/js/game.js', 'utf8');

try {
    const scriptEl = document.createElement('script');
    scriptEl.textContent = scriptCode;
    document.body.appendChild(scriptEl);
    console.log("Script evaluated successfully without top-level errors.");
    
    // Trigger DOMContentLoaded
    const event = document.createEvent('Event');
    event.initEvent('DOMContentLoaded', true, true);
    document.dispatchEvent(event);
    console.log("DOMContentLoaded dispatched.");
    
} catch (e) {
    console.error("ERROR:", e);
}
