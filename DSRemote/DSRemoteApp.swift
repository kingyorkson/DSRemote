import SwiftUI

@main
struct DSRemoteApp: App {
    @StateObject private var settings = AppSettings()
    @StateObject private var network = NetworkService()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(settings)
                .environmentObject(network)
                .preferredColorScheme(.dark)
        }
    }
}
