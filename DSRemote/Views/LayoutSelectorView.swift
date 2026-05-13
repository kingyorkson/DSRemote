import SwiftUI

struct LayoutSelectorView: View {
    @EnvironmentObject private var layoutService: LayoutService
    @Environment(\.dismiss) private var dismiss
    @State private var showEditor = false
    @State private var editingLayout: SavedLayout?

    var body: some View {
        NavigationView {
            ZStack {
                Color(hex: "#1a1a2e").ignoresSafeArea()

                VStack(spacing: 0) {
                    Text("Customize Layout")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundColor(.white)
                        .padding(.top, 20)

                    Text("Select or create a button layout")
                        .font(.subheadline)
                        .foregroundColor(.gray)
                        .padding(.bottom, 20)

                    ScrollView {
                        VStack(spacing: 12) {
                            ForEach(layoutService.layouts) { layout in
                                layoutRow(layout)
                            }
                        }
                        .padding(.horizontal)
                    }

                    Button(action: {
                        var newLayout = DefaultLayout
                        newLayout.id = UUID()
                        newLayout.name = "Layout \(layoutService.layouts.count + 1)"
                        editingLayout = newLayout
                        showEditor = true
                    }) {
                        HStack {
                            Image(systemName: "plus.circle.fill")
                            Text("Create New Layout")
                        }
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color(hex: "#16213e"))
                        .foregroundColor(.white)
                        .cornerRadius(12)
                        .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color(hex: "#0f3460"), lineWidth: 1))
                    }
                    .padding()
                }
            }
            .navigationBarHidden(true)
        }
        .fullScreenCover(isPresented: $showEditor) {
            if let layout = editingLayout {
                LayoutEditorView(layout: layout)
                    .environmentObject(layoutService)
            }
        }
    }

    private func layoutRow(_ layout: SavedLayout) -> some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(layout.name)
                    .font(.headline)
                    .foregroundColor(.white)
                Text("\(layout.buttons.count) buttons")
                    .font(.caption)
                    .foregroundColor(.gray)
            }

            Spacer()

            if layoutService.activeLayoutId == layout.id {
                Text("Active")
                    .font(.caption)
                    .fontWeight(.bold)
                    .foregroundColor(.green)
                    .padding(.horizontal, 10)
                    .padding(.vertical, 4)
                    .background(Color.green.opacity(0.2))
                    .cornerRadius(6)
            }

            Button(action: {
                layoutService.activeLayoutId = layout.id
                layoutService.save()
            }) {
                Text("Select")
                    .font(.caption)
                    .fontWeight(.semibold)
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(layoutService.activeLayoutId == layout.id ? Color.gray : Color(hex: "#0f3460"))
                    .foregroundColor(.white)
                    .cornerRadius(6)
            }
            .disabled(layoutService.activeLayoutId == layout.id)

            Button(action: {
                editingLayout = layout
                showEditor = true
            }) {
                Image(systemName: "pencil")
                    .foregroundColor(.gray)
                    .padding(8)
                    .background(Color(hex: "#16213e"))
                    .cornerRadius(6)
            }
        }
        .padding(12)
        .background(Color(hex: "#16213e"))
        .cornerRadius(10)
    }
}
