import SwiftUI

@main
struct DSRemoteApp: App {
    @StateObject private var settings = AppSettings()
    @StateObject private var network = NetworkService()
    @StateObject private var layoutService = LayoutService()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(settings)
                .environmentObject(network)
                .environmentObject(layoutService)
                .preferredColorScheme(.dark)
        }
    }
}
