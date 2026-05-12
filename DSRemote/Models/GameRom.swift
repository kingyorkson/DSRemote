import Foundation

struct GameRom: Identifiable, Codable {
    let id = UUID()
    let name: String
    let fullPath: String
    let platform: String
    let sizeFormatted: String
}
