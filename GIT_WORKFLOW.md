# Git Workflow & Standards - Project: AntiGravity

## 1. Cấu trúc và Quy định Nhánh (Branching Strategy & Rules)
- **QUY ĐỊNH BẮT BUỘC:** Bất cứ khi nào bắt đầu làm một chức năng (Use Case - UC) mới, lập trình viên ĐỀU PHẢI tạo một nhánh mới biệt lập. Tuyệt đối không code trực tiếp trên `main` hoặc `develop`.
- `main` / `master`: Nhánh production, chứa code ổn định nhất, build cho Windows/Android.
- `develop`: Nhánh tích hợp chính. Code đang phát triển sẽ được tập hợp tại đây.
- `uc/<mã-UC>-<tên-chức-năng>`: Nhánh tạo mới để phát triển chức năng (UC) (VD: `uc/UC01-player-movement`).
- `fix-uc/<mã-UC>-<tên-chức-năng>`: Nhánh tạo mới (tách ra từ nhánh UC đã push) để sửa lỗi hoặc cập nhật lại chức năng đó (VD: `fix-uc/UC01-player-movement`).
- `bugfix/<tên-lỗi>`: Nhánh sửa lỗi phát sinh chung trong quá trình dev từ nhánh `develop`.
- `hotfix/<tên-lỗi>`: Nhánh sửa lỗi khẩn cấp trực tiếp từ nhánh `main`.

## 2. Quy tắc Commit (Conventional Commits)
Sử dụng các tiền tố sau khi viết commit message:
- `feat:` Thêm tính năng mới (VD: `feat: add player jump logic with fusion`).
- `fix:` Sửa lỗi (VD: `fix: resolve timeout disconnect issue`).
- `refactor:` Tối ưu code, cấu trúc lại code mà không thay đổi logic hiện tại.
- `chore:` Thay đổi cấu hình, build process, hoặc các tác vụ không liên quan trực tiếp đến code game.
- `docs:` Cập nhật tài liệu (VD: `docs: update .cursorrules`).

## 3. Quy trình làm việc cơ bản (Workflow)
1. Cập nhật nhánh develop: `git checkout develop` -> `git pull origin develop`
2. **Bắt đầu UC mới (Tạo nhánh mới):** `git checkout -b uc/<mã-UC>-<tên-chức-năng>`
3. Code và commit thường xuyên theo quy tắc ở mục 2.
4. Push nhánh lên remote: `git push origin uc/<mã-UC>-<tên-chức-năng>`
5. **(Nếu cần sửa lại UC đã push, tạo nhánh sửa lỗi mới):** `git checkout -b fix-uc/<mã-UC>-<tên-chức-năng>`, tiến hành code, commit và push lên remote.
6. Tạo Pull Request (PR) để merge nhánh hoàn thiện vào `develop`.
7. Xóa nhánh ở local và remote sau khi đã merge thành công.

## 4. Xung đột (Merge Conflicts) trong Unity
- **Cảnh báo:** Tuyệt đối không nhiều người cùng sửa một file `.scene` hoặc `.prefab` cùng một lúc.
- Luôn chia nhỏ UI hoặc Object thành các Prefab riêng biệt để tránh conflict.
- Nếu xảy ra conflict ở file scene/prefab, ưu tiên sử dụng công cụ UnityYAMLMerge.