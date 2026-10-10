# STEP 05B - Kiểm thử đồng thời kết quả Đạt

Chạy API và thay token/ID bên dưới bằng dữ liệu thử nghiệm. Tạo sẵn hai đơn đang mở cho cùng một sinh viên nhưng thuộc hai doanh nghiệp trong lượt 1. Với lượt 2, tạo hai đơn của hai sinh viên khác nhau cho cùng một vị trí `Approved` còn đúng một chỉ tiêu (`Quantity = 1`).

Trong PowerShell, đặt URL và hàm gọi:

```powershell
$baseUrl = "http://localhost:5018/api/company-me/placement-requests"
$requestA = 101
$requestB = 102
$tokenA = "REPLACE_WITH_COMPANY_A_BEARER_TOKEN"
$tokenB = "REPLACE_WITH_COMPANY_B_BEARER_TOKEN"
$companyToken = "REPLACE_WITH_COMPANY_BEARER_TOKEN"

$sendPassResult = {
    param($baseUrl, $requestId, $token)
    try {
        $response = Invoke-WebRequest `
            -Method Post `
            -Uri "$baseUrl/$requestId/result" `
            -Headers @{ Authorization = "Bearer $token" } `
            -ContentType "application/json" `
            -Body '{"isPassed":true,"note":"Đạt"}'
        "HTTP $([int]$response.StatusCode) - request $requestId"
    } catch {
        $status = [int]$_.Exception.Response.StatusCode
        "HTTP $status - request $requestId"
    }
}
```

## Hai doanh nghiệp cùng nhận một sinh viên

`$requestA` và `$requestB` là đơn của cùng sinh viên; mỗi token phải thuộc đúng doanh nghiệp của đơn:

```powershell
$jobA = Start-Job -ScriptBlock $sendPassResult -ArgumentList $baseUrl, $requestA, $tokenA
$jobB = Start-Job -ScriptBlock $sendPassResult -ArgumentList $baseUrl, $requestB, $tokenB
Wait-Job $jobA, $jobB | Out-Null
Receive-Job $jobA, $jobB
Remove-Job $jobA, $jobB
```

## Hai sinh viên cùng nhận chỉ tiêu cuối

`$request1` và `$request2` là đơn của hai sinh viên cho cùng vị trí; dùng token của doanh nghiệp sở hữu vị trí:

```powershell
$request1 = 201
$request2 = 202
$job1 = Start-Job -ScriptBlock $sendPassResult -ArgumentList $baseUrl, $request1, $companyToken
$job2 = Start-Job -ScriptBlock $sendPassResult -ArgumentList $baseUrl, $request2, $companyToken
Wait-Job $job1, $job2 | Out-Null
Receive-Job $job1, $job2
Remove-Job $job1, $job2
```

Mỗi cặp mong đợi đúng một phản hồi thành công `200` và một `409 Conflict`. Với lượt chỉ tiêu, vị trí phải được đóng sau khi kết quả thành công.
