import Foundation
import Network

@MainActor
class NetworkService: ObservableObject {
    @Published var isConnected = false
    @Published var connectionStatus = "Disconnected"
    @Published var games: [GameRom] = []

    private var connection: NWConnection?
    private var host: NWEndpoint.Host = "127.0.0.1"
    private var port: NWEndpoint.Port = 9876
    private var onScreenshot: ((Data) -> Void)?
    private var onDisconnect: (() -> Void)?

    private var receivedBuffer = Data()

    func configure(host: String, port: Int, onScreenshot: @escaping (Data) -> Void, onDisconnect: @escaping () -> Void) {
        self.host = NWEndpoint.Host(host)
        self.port = NWEndpoint.Port(UInt16(port))
        self.onScreenshot = onScreenshot
        self.onDisconnect = onDisconnect
    }

    func connect() {
        connectionStatus = "Connecting..."
        let tcpOptions = NWProtocolTCP.Options()
        tcpOptions.keepaliveCount = 3
        tcpOptions.keepaliveIdle = 5
        tcpOptions.keepaliveInterval = 5

        let params = NWParameters(tls: nil, tcp: tcpOptions)
        connection = NWConnection(host: host, port: port, using: params)

        connection?.stateUpdateHandler = { [weak self] state in
            DispatchQueue.main.async {
                switch state {
                case .ready:
                    self?.isConnected = true
                    self?.connectionStatus = "Connected"
                    self?.receiveLoop()
                    self?.sendDeviceInfo()
                case .failed(let error):
                    self?.isConnected = false
                    self?.connectionStatus = "Failed: \(error.localizedDescription)"
                    self?.onDisconnect?()
                case .cancelled:
                    self?.isConnected = false
                    self?.connectionStatus = "Disconnected"
                    self?.onDisconnect?()
                case .waiting(let error):
                    self?.connectionStatus = "Waiting: \(error.localizedDescription)"
                default:
                    break
                }
            }
        }

        connection?.start(queue: .main)
    }

    func disconnect() {
        connection?.cancel()
        connection = nil
        isConnected = false
        connectionStatus = "Disconnected"
        games = []
    }

    private func receiveLoop() {
        connection?.receive(minimumIncompleteLength: 1, maximumLength: 65536) { [weak self] data, _, isComplete, error in
            guard let self = self else { return }

            if let data = data, !data.isEmpty {
                self.processData(data)
            }

            if isComplete || error != nil {
                DispatchQueue.main.async {
                    self.isConnected = false
                    self.connectionStatus = "Disconnected"
                    self.onDisconnect?()
                }
                return
            }

            self.receiveLoop()
        }
    }

    private func processData(_ data: Data) {
        receivedBuffer.append(data)

        let imgMarker = "IMAGE:".data(using: .utf8)!
        while true {
            if receivedBuffer.count >= imgMarker.count,
               receivedBuffer[..<imgMarker.count] == imgMarker {
                let rest = receivedBuffer[imgMarker.count...]
                if let endIndex = rest.firstIndex(of: UInt8(ascii: "\n")) ?? rest.firstIndex(of: UInt8(ascii: "\0")) {
                    let base64Data = rest[..<endIndex]
                    if let base64Str = String(data: base64Data, encoding: .utf8),
                       let imgData = Data(base64Encoded: base64Str) {
                        onScreenshot?(imgData)
                    }
                    receivedBuffer = Data(rest[endIndex+1...])
                    continue
                }
            }

            if let json = try? JSONSerialization.jsonObject(with: receivedBuffer) as? [String: Any] {
                if let gamesData = json["games"] as? [[String: Any]] {
                    parseGames(gamesData)
                } else if let message = json["message"] as? String {
                    DispatchQueue.main.async {
                        self.connectionStatus = message
                    }
                }
                receivedBuffer = Data()
                continue
            }
            break
        }
    }

    private func parseGames(_ gamesData: [[String: Any]]) {
        var parsed: [GameRom] = []
        for g in gamesData {
            if let name = g["name"] as? String,
               let path = g["fullPath"] as? String,
               let platform = g["platform"] as? String,
               let size = g["sizeFormatted"] as? String {
                parsed.append(GameRom(name: name, fullPath: path, platform: platform, sizeFormatted: size))
            }
        }
        DispatchQueue.main.async {
            self.games = parsed
        }
    }

    func sendInput(_ type: InputType, args: [Float]) {
        let event = InputEvent(type: type, args: args)
        guard let data = try? JSONEncoder().encode(event) else { return }
        send(data)
    }

    func sendLaunchGame(_ game: GameRom) {
        let msg = ["action": "launch", "path": game.fullPath]
        guard let data = try? JSONSerialization.data(withJSONObject: msg) else { return }
        send(data)
    }

    func sendDisconnect() {
        let msg = ["action": "disconnect"]
        guard let data = try? JSONSerialization.data(withJSONObject: msg) else { return }
        send(data)
    }

    private func sendDeviceInfo() {
        let info = ["action": "deviceInfo", "name": UIDevice.current.name]
        guard let data = try? JSONSerialization.data(withJSONObject: info) else { return }
        send(data)
    }

    private func send(_ data: Data) {
        connection?.send(content: data, completion: .contentProcessed { _ in })
    }
}
