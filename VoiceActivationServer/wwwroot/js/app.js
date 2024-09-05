"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
let socket;
let mediaRecorder;
const startRecognitionButton = document.getElementById('startRecognition');
const stopRecognitionButton = document.getElementById('stopRecognition');
const statusElement = document.getElementById('status'); // Rename to avoid conflict
// Start recognition
startRecognitionButton.addEventListener('click', () => __awaiter(void 0, void 0, void 0, function* () {
    socket = new WebSocket('wss://localhost:7014/ws'); // WebSocket server URL
    // WebSocket open event
    socket.onopen = () => {
        console.log("Connected to WebSocket server");
        statusElement.textContent = "Connected to WebSocket server"; // Update name
        startAudioStreaming();
    };
    // WebSocket message event
    socket.onmessage = (event) => {
        console.log("Server response: ", event.data);
        statusElement.textContent = `Keyword detected: ${event.data}`; // Update name
    };
    // WebSocket close event
    socket.onclose = () => {
        console.log("Disconnected from WebSocket server");
        statusElement.textContent = "Disconnected"; // Update name
    };
}));
// Stop recognition
stopRecognitionButton.addEventListener('click', () => {
    if (mediaRecorder) {
        mediaRecorder.stop();
        socket.close();
        statusElement.textContent = "Stopped recognition"; // Update name
    }
});
// Start streaming audio via WebSocket
function startAudioStreaming() {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            const stream = yield navigator.mediaDevices.getUserMedia({ audio: true });
            const options = { mimeType: 'audio/webm' };
            mediaRecorder = new MediaRecorder(stream, options);
            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0 && socket.readyState === WebSocket.OPEN) {
                    socket.send(event.data);
                }
            };
            mediaRecorder.start(250); // Stream audio in chunks every 250ms
            statusElement.textContent = "Streaming audio..."; // Update name
        }
        catch (err) {
            console.error("Error capturing audio: ", err);
            statusElement.textContent = "Error capturing audio"; // Update name
        }
    });
}
