import SwiftUI

struct SavedLayout: Codable, Identifiable {
    var id = UUID()
    var name: String
    var buttons: [ButtonConfig]
}

struct ButtonConfig: Codable, Identifiable {
    var id: String
    var label: String
    var colorHex: String
    var relX: Double
    var relY: Double
    var relSize: Double

    var color: Color { Color(hex: colorHex) }
}

let DefaultButtonConfigs = [
    ButtonConfig(id: "a", label: "A", colorHex: "#4CAF50", relX: 0.75, relY: 0.6, relSize: 0.12),
    ButtonConfig(id: "b", label: "B", colorHex: "#F44336", relX: 0.6, relY: 0.7, relSize: 0.12),
    ButtonConfig(id: "x", label: "X", colorHex: "#2196F3", relX: 0.6, relY: 0.5, relSize: 0.12),
    ButtonConfig(id: "y", label: "Y", colorHex: "#FFC107", relX: 0.75, relY: 0.4, relSize: 0.12),
    ButtonConfig(id: "l", label: "L", colorHex: "#9E9E9E", relX: 0.2, relY: 0.15, relSize: 0.1),
    ButtonConfig(id: "r", label: "R", colorHex: "#9E9E9E", relX: 0.7, relY: 0.15, relSize: 0.1),
    ButtonConfig(id: "start", label: "Start", colorHex: "#607D8B", relX: 0.55, relY: 0.85, relSize: 0.08),
    ButtonConfig(id: "select", label: "Select", colorHex: "#607D8B", relX: 0.4, relY: 0.85, relSize: 0.08),
    ButtonConfig(id: "dpad", label: "DPad", colorHex: "#555555", relX: 0.2, relY: 0.55, relSize: 0.18),
    ButtonConfig(id: "joystick", label: "Stick", colorHex: "#555555", relX: 0.2, relY: 0.35, relSize: 0.14),
]

let DefaultLayout = SavedLayout(name: "Default", buttons: DefaultButtonConfigs)

class LayoutService: ObservableObject {
    @Published var layouts: [SavedLayout] = []
    @Published var activeLayoutId: UUID?
    @Published var showSelector = false

    private let layoutsKey = "savedLayouts"
    private let activeKey = "activeLayoutId"

    init() {
        load()
        if layouts.isEmpty {
            layouts = [DefaultLayout]
            activeLayoutId = DefaultLayout.id
            save()
        }
    }

    var activeLayout: SavedLayout {
        layouts.first(where: { $0.id == activeLayoutId }) ?? DefaultLayout
    }

    func save() {
        if let data = try? JSONEncoder().encode(layouts) {
            UserDefaults.standard.set(data, forKey: layoutsKey)
        }
        UserDefaults.standard.set(activeLayoutId?.uuidString, forKey: activeKey)
    }

    private func load() {
        if let data = UserDefaults.standard.data(forKey: layoutsKey),
           let decoded = try? JSONDecoder().decode([SavedLayout].self, from: data) {
            layouts = decoded
        }
        if let idStr = UserDefaults.standard.string(forKey: activeKey),
           let id = UUID(uuidString: idStr) {
            activeLayoutId = id
        }
    }

    func delete(_ layout: SavedLayout) {
        layouts.removeAll(where: { $0.id == layout.id })
        if activeLayoutId == layout.id {
            activeLayoutId = layouts.first?.id
        }
        save()
    }

    func duplicate(_ layout: SavedLayout) {
        var copy = layout
        copy.id = UUID()
        copy.name = "\(layout.name) Copy"
        layouts.append(copy)
        save()
    }

    func restoreDefault() {
        layouts = [DefaultLayout]
        activeLayoutId = DefaultLayout.id
        save()
    }
}
