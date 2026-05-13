import Foundation
import Network
import UIKit

enum ConnectionType: String {
    case wifi = "Wi-Fi"
    case usb = "USB"
}

@MainActor
class NetworkService: ObservableObject {
    @Published var isConnected = false
    @Published var connectionStatus = "Disconnected"
    @Published var games: [GameRom] = []
    @Published var discoveredHosts: [String] = []
    @Published var connectionType: ConnectionType = .wifi

    private var connection: NWConnection?
    private var host: NWEndpoint.Host = "127.0.0.1"
    private var port: NWEndpoint.Port = 9876
    private var onScreenshot: ((Data) -> Void)?
    private var onDisconnect: (() -> Void)?
    private var onAutoConnect: (() -> Void)?

    private var udpListener: NWListener?

    private var receivedBuffer = Data()

    func configure(host: String, port: Int, onScreenshot: @escaping (Data) -> Void, onDisconnect: @escaping () -> Void) {
        self.host = NWEndpoint.Host(host)
        self.port = NWEndpoint.Port(rawValue: UInt16(port)) ?? 9876
        self.onScreenshot = onScreenshot
        self.onDisconnect = onDisconnect
    }

    func setAutoConnectHandler(_ handler: @escaping () -> Void) {
        onAutoConnect = handler
    }

    var onTopScreenshot: ((Data) -> Void)?
    var onBottomScreenshot: ((Data) -> Void)?

    func startDiscovery() {
        guard let listener = try? NWListener(using: .udp, on: 9877) else { return }
        udpListener = listener

        listener.newConnectionHandler = { conn in
            conn.receiveMessage { data, _, _, _ in
                if let data = data,
                   let msg = String(data: data, encoding: .utf8),
                   msg.hasPrefix("DSREMOTE:") {
                    let parts = msg.dropFirst(9).split(separator: ":")
                    if parts.count >= 2 {
                        let ip = String(parts[0])
                        let transportType: ConnectionType = parts.count >= 4 && parts[3] == "USB" ? .usb : .wifi

                        DispatchQueue.main.async {
                            if transportType == .usb {
                                // Auto-connect via USB
                                self.host = NWEndpoint.Host(ip)
                                self.connect()
                                self.onAutoConnect?()
                            } else {
                                // WiFi: show in list for manual tap
                                if !self.discoveredHosts.contains(ip) {
                                    self.discoveredHosts.append(ip)
                                }
                            }
                        }
                    }
                }
                conn.cancel()
            }
            conn.start(queue: .main)
        }
        listener.start(queue: .main)
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
            Task { @MainActor in
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
        connectionType = .wifi
        games = []
    }

    private func receiveLoop() {
        connection?.receive(minimumIncompleteLength: 1, maximumLength: 65536) { [weak self] data, _, isComplete, error in
            Task { @MainActor [weak self] in
                guard let self = self else { return }

                if let data = data, !data.isEmpty {
                    self.processData(data)
                }

                if isComplete || error != nil {
                    self.isConnected = false
                    self.connectionStatus = "Disconnected"
                    self.onDisconnect?()
                    return
                }

                self.receiveLoop()
            }
        }
    }

    private func processData(_ data: Data) {
        receivedBuffer.append(data)

        let markers: [(Data, (Data) -> Void)] = [
            ("BOTTOM_IMAGE:".data(using: .utf8)!, { [weak self] in self?.onBottomScreenshot?($0) ?? self?.onScreenshot?($0) }),
            ("TOP_IMAGE:".data(using: .utf8)!, { [weak self] in self?.onTopScreenshot?($0) ?? self?.onScreenshot?($0) }),
            ("IMAGE:".data(using: .utf8)!, { [weak self] in self?.onScreenshot?($0) }),
        ]

        while true {
            var matched = false
            for (marker, handler) in markers {
                guard receivedBuffer.count >= marker.count,
                      receivedBuffer[..<marker.count] == marker else { continue }
                let rest = receivedBuffer[marker.count...]
                if let endIndex = rest.firstIndex(of: UInt8(ascii: "\n")) ?? rest.firstIndex(of: UInt8(ascii: "\0")) {
                    let base64Data = rest[..<endIndex]
                    if let base64Str = String(data: base64Data, encoding: .utf8),
                       let imgData = Data(base64Encoded: base64Str) {
                        handler(imgData)
                    }
                    receivedBuffer = Data(rest[rest.index(after: endIndex)...])
                    matched = true
                    break
                }
            }
            if !matched { break }
        }

        while true {
            guard let newlineIndex = receivedBuffer.firstIndex(of: UInt8(ascii: "\n")) else { break }
            let jsonData = receivedBuffer[..<newlineIndex]
            receivedBuffer = Data(receivedBuffer[receivedBuffer.index(after: newlineIndex)...])

            guard !jsonData.isEmpty,
                  let json = try? JSONSerialization.jsonObject(with: jsonData) as? [String: Any] else { continue }

            if let action = json["action"] as? String {
                if action == "disconnected" {
                    isConnected = false
                    connectionStatus = "Disconnected by server"
                    onDisconnect?()
                } else if action == "welcome", let connType = json["connection"] as? String {
                    connectionType = connType == "usb" ? .usb : .wifi
                    connectionStatus = "Connected via \(connectionType.rawValue)"
                }
            } else if let gamesData = json["games"] as? [[String: Any]] {
                parseGames(gamesData)
            } else if let message = json["message"] as? String {
                connectionStatus = message
            }
        }
    }

    private func parseGames(_ gamesData: [[String: Any]]) {
        var parsed: [GameRom] = []
        for g in gamesData {
            guard let name = (g["name"] as? String) ?? (g["Name"] as? String),
                  let path = (g["fullPath"] as? String) ?? (g["FullPath"] as? String) else { continue }
            let platform: String = {
                if let s = g["platform"] as? String ?? g["Platform"] as? String { return s }
                if let n = g["platform"] as? Int ?? g["Platform"] as? Int { return n == 0 ? "DS" : "ThreeDS" }
                return ""
            }()
            let size = (g["sizeFormatted"] as? String ?? g["SizeFormatted"] as? String) ?? ""
            parsed.append(GameRom(name: name, fullPath: path, platform: platform, sizeFormatted: size))
        }
        games = parsed
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

    func sendStopEmulation() {
        let msg = ["action": "stop"]
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
