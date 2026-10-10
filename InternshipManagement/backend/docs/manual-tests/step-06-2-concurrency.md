# STEP 06-2 - Kiểm thử đồng thời xác nhận kết quả phỏng vấn

Chạy API và chuẩn bị một đơn `InterviewInvited` đã có `StudentReportedInterviewAt`. Dùng tài khoản Admin và cùng một `requestId` cho cả hai request trong mỗi lượt.

```powershell
$baseUrl = "http://localhost:5018/api/placement-requests"
$requestId = 301
$adminToken = "REPLACE_WITH_ADMIN_BEARER_TOKEN"

$sendAdminResult = {
    param($baseUrl, $requestId, $adminToken, $body)
    try {
        $response = Invoke-WebRequest `
            -Method Post `
            -Uri "$baseUrl/$requestId/admin-confirm-result" `
            -Headers @{ Authorization = "Bearer $adminToken" } `
            -ContentType "application/json" `
            -Body $body
        "HTTP $([int]$response.StatusCode)"
    } catch {
        $status = [int]$_.Exception.Response.StatusCode
        "HTTP $status"
    }
}
```

## Hai quyết định Failed đồng thời

```powershell
$body = '{"decision":1,"confirmationSource":"Đã xác minh với doanh nghiệp","note":"Không đạt"}'
$job1 = Start-Job -ScriptBlock $sendAdminResult -ArgumentList $baseUrl, $requestId, $adminToken, $body
$job2 = Start-Job -ScriptBlock $sendAdminResult -ArgumentList $baseUrl, $requestId, $adminToken, $body
Wait-Job $job1, $job2 | Out-Null
Receive-Job $job1, $job2
Remove-Job $job1, $job2
```

## Failed và DismissReport đồng thời

```powershell
$failed = '{"decision":1,"confirmationSource":"Đã xác minh với doanh nghiệp","note":"Không đạt"}'
$dismiss = '{"decision":2,"confirmationSource":"Đã xác minh với doanh nghiệp","note":"Báo cáo không hợp lệ"}'
$job1 = Start-Job -ScriptBlock $sendAdminResult -ArgumentList $baseUrl, $requestId, $adminToken, $failed
$job2 = Start-Job -ScriptBlock $sendAdminResult -ArgumentList $baseUrl, $requestId, $adminToken, $dismiss
Wait-Job $job1, $job2 | Out-Null
Receive-Job $job1, $job2
Remove-Job $job1, $job2
```

Trong mỗi lượt phải có đúng một `200` và một lỗi `400` hoặc `409`. Kiểm tra dữ liệu sau đó để xác nhận đơn chỉ được xử lý một lần.
