let socket: WebSocket;
let mediaRecorder: MediaRecorder;

const startRecognitionButton = document.getElementById('startRecognition')!;
const stopRecognitionButton = document.getElementById('stopRecognition')!;
const statusElement = document.getElementById('status')!; 

startRecognitionButton.addEventListener('click', async () => {
    socket = new WebSocket('wss://localhost:7014/ws');  // Note: Replace WebSocket server URL correctly    
    socket.onopen = () => {
        console.log("Connected to WebSocket server");
        statusElement.textContent = "Connected to WebSocket server";  
        startAudioStreaming();
    };

    socket.onmessage = (event) => {
        console.log("Server response: ", event.data);
        statusElement.textContent = `Keyword detected: ${event.data}`;  
    };
    
    socket.onclose = () => {
        console.log("Disconnected from WebSocket server");
        statusElement.textContent = "Disconnected"; 
    };
});

stopRecognitionButton.addEventListener('click', () => {
    if (mediaRecorder) {
        mediaRecorder.stop();
        socket.close();
        statusElement.textContent = "Stopped recognition";  
    }
});

async function startAudioStreaming() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const options = { mimeType: 'audio/webm' };
        mediaRecorder = new MediaRecorder(stream, options);

        mediaRecorder.ondataavailable = (event: BlobEvent) => {
            if (event.data.size > 0 && socket.readyState === WebSocket.OPEN) {
                socket.send(event.data);
            }
        };

        mediaRecorder.start(250);  
        statusElement.textContent = "Streaming audio...";  
    } catch (err) {
        console.error("Error capturing audio: ", err);
        statusElement.textContent = "Error capturing audio";  
    }
}
