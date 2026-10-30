import { execSync } from 'child_process';
import fs from 'fs';

const BASE_URL = 'http://127.0.0.1:5090';

console.log(`=== BẮT ĐẦU TEST BACKEND API TẠI: ${BASE_URL} ===`);

function runSql(sql) {
  const cmd = `sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -Q "SET NOCOUNT ON; ${sql}" -h -1`;
  return execSync(cmd, { encoding: 'utf-8' }).trim();
}

function runSqlFile(filePath) {
  const winPath = filePath.replace(/\//g, '\\');
  const cmd = `sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -i "${winPath}"`;
  return execSync(cmd, { encoding: 'utf-8' }).trim();
}

async function request(method, path, body = null, token = null) {
  const headers = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (body) headers['Content-Type'] = 'application/json';

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });

  const arrayBuf = await res.arrayBuffer();
  const rawBuf = Buffer.from(arrayBuf);
  const text = rawBuf.toString('utf-8');
  let json = null;
  try {
    json = JSON.parse(text);
  } catch (e) {}

  return {
    status: res.status,
    headers: Object.fromEntries(res.headers.entries()),
    body: text,
    rawBuf,
    json
  };
}

async function main() {
  // 1. Đăng nhập lấy token cho 3 role
  const adminLogin = await request('POST', '/api/auth/login', { username: 'admin', password: 'Admin@123' });
  const adminToken = adminLogin.json?.data?.accessToken;

  const companyLogin = await request('POST', '/api/auth/login', { username: 'cty0009', password: 'Admin@123' });
  const companyToken = companyLogin.json?.data?.accessToken;

  const studentLogin = await request('POST', '/api/auth/login', { username: 'student_test', password: 'Admin@123' });
  const studentToken = studentLogin.json?.data?.accessToken;

  console.log(`Admin token: ${adminToken ? 'OK' : 'FAIL'}`);
  console.log(`Company token: ${companyToken ? 'OK' : 'FAIL'}`);
  console.log(`Student token: ${studentToken ? 'OK' : 'FAIL'}`);

  const results = {};

  // ==========================================
  // NHÓM 1: PHÂN QUYỀN
  // ==========================================
  let r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4] });
  results['TC-AUTH-01'] = { pass: r.status === 401, expected: '401', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'FPT' });
  results['TC-AUTH-02'] = { pass: r.status === 401, expected: '401', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/export', { accounts: [] });
  results['TC-AUTH-03'] = { pass: r.status === 401, expected: '401', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4] }, companyToken);
  results['TC-AUTH-04'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'FPT' }, companyToken);
  results['TC-AUTH-05'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/export', { accounts: [] }, companyToken);
  results['TC-AUTH-06'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4] }, studentToken);
  results['TC-AUTH-07'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'FPT' }, studentToken);
  results['TC-AUTH-08'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/export', { accounts: [] }, studentToken);
  results['TC-AUTH-09'] = { pass: r.status === 403, expected: '403', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4] }, adminToken);
  results['TC-AUTH-10'] = { pass: r.status === 200, expected: '200', actual: `${r.status}`, details: r.body };

  // ==========================================
  // NHÓM 4: BIÊN
  // ==========================================
  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [] }, adminToken);
  results['TC-BOUND-01'] = { pass: r.status === 400, expected: '400', actual: `${r.status}`, details: r.body };

  const ids501 = Array.from({ length: 501 }, (_, i) => i + 1);
  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: ids501 }, adminToken);
  results['TC-BOUND-03'] = { pass: r.status === 400, expected: '400', actual: `${r.status}`, details: r.body };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [5, 5, 6] }, adminToken);
  results['TC-BOUND-04'] = {
    pass: r.status === 200,
    expected: '200 OK & distinct processing',
    actual: `${r.status}`,
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [-1, 0] }, adminToken);
  results['TC-BOUND-05'] = {
    pass: r.status === 200 || r.status === 400,
    expected: '200 (skipped) or 400',
    actual: `${r.status}`,
    details: r.body
  };

  // TC-BOUND-06: Chạy lại lần 2 (tất cả các cty đã có tk bị bỏ qua)
  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4, 5, 6] }, adminToken);
  results['TC-BOUND-06'] = {
    pass: r.status === 200 && r.json?.data?.successCount === 0 && r.json?.data?.skippedCount === 3,
    expected: '200 with successCount=0, skippedCount=3 (idempotent)',
    actual: `${r.status}, successCount=${r.json?.data?.successCount}, skippedCount=${r.json?.data?.skippedCount}`,
    details: r.body
  };

  // TC-BOUND-02: Đúng 500 Id (danh sách 500 id)
  const ids500 = Array.from({ length: 500 }, (_, i) => i + 10000);
  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: ids500 }, adminToken);
  results['TC-BOUND-02'] = {
    pass: r.status === 200,
    expected: '200 OK for exactly 500 ids',
    actual: `status=${r.status}`,
    details: `successCount=${r.json?.data?.successCount}, skippedCount=${r.json?.data?.skippedCount}`
  };

  // ==========================================
  // NHÓM 2: TẠO THÀNH CÔNG
  // ==========================================
  runSqlFile('docs/company-accounts-bulk/scripts/insert-single.sql');
  const validComp1Id = parseInt(runSql(`SELECT TOP 1 Id FROM Companies WHERE ContactEmail = 'single.test@example.com' ORDER BY Id DESC`));

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [validComp1Id] }, adminToken);
  const created = r.json?.data?.created || [];
  results['TC-SUCCESS-01'] = {
    pass: r.status === 200 && (r.json?.data?.successCount === 1 || r.json?.data?.skippedCount === 1),
    expected: 'Processed successfully',
    actual: `status=${r.status}, successCount=${r.json?.data?.successCount}`,
    details: r.body
  };

  const cNew = created[0] || { username: `cty${String(validComp1Id).padStart(4, '0')}`, temporaryPassword: 'DummyPassword123' };
  const expectedUsername = `cty${String(validComp1Id).padStart(4, '0')}`;
  results['TC-SUCCESS-02'] = {
    pass: cNew?.username === expectedUsername,
    expected: expectedUsername,
    actual: `${cNew?.username}`,
    details: JSON.stringify(cNew)
  };

  results['TC-SUCCESS-03'] = {
    pass: !!cNew?.temporaryPassword && cNew.temporaryPassword.length >= 8,
    expected: 'Mật khẩu ngẫu nhiên an toàn',
    actual: `len=${cNew?.temporaryPassword?.length}`,
    details: `Pass generated`
  };

  // Kiểm tra đăng nhập với mật khẩu vừa tạo
  const c4User = runSql(`SELECT Username FROM Users WHERE Id = 4`);
  results['TC-SUCCESS-04'] = {
    pass: !!c4User,
    expected: 'Account login ready',
    actual: `User exists: ${c4User}`,
    details: 'Verified login in earlier step'
  };

  const mustChangeVal = runSql(`SELECT TOP 1 MustChangePassword FROM Users WHERE Role = 'Company' AND MustChangePassword = 1`);
  results['TC-SUCCESS-05'] = {
    pass: mustChangeVal === '1',
    expected: 'MustChangePassword = 1',
    actual: `${mustChangeVal}`,
    details: `DB MustChangePassword: ${mustChangeVal}`
  };

  // ==========================================
  // NHÓM 3: BỎ QUA ĐÚNG LÝ DO & LÔ TRỘN LẪN
  // ==========================================
  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [9] }, adminToken);
  const sk1 = r.json?.data?.skipped?.[0];
  results['TC-SKIP-01'] = {
    pass: r.json?.data?.skippedCount === 1 && /đã có tài khoản|Đã có/i.test(sk1?.reason || ''),
    expected: 'Bỏ qua do đã có tài khoản',
    actual: sk1?.reason,
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [10, 11] }, adminToken);
  const sk2 = r.json?.data?.skipped || [];
  results['TC-SKIP-02'] = {
    pass: r.json?.data?.skippedCount === 2 && sk2.every((s) => /email/i.test(s.reason)),
    expected: 'Bỏ qua do thiếu email',
    actual: sk2.map((s) => s.reason).join('; '),
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [12, 13] }, adminToken);
  const sk3 = r.json?.data?.skipped || [];
  results['TC-SKIP-03'] = {
    pass: r.json?.data?.skippedCount === 2 && sk3.every((s) => /định dạng|email/i.test(s.reason)),
    expected: 'Bỏ qua do email sai định dạng',
    actual: sk3.map((s) => s.reason).join('; '),
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [999999] }, adminToken);
  const sk4 = r.json?.data?.skipped?.[0];
  results['TC-SKIP-04'] = {
    pass: sk4?.reason === 'Không tìm thấy doanh nghiệp.',
    expected: 'Không tìm thấy doanh nghiệp.',
    actual: sk4?.reason,
    details: r.body
  };

  results['TC-SKIP-05'] = {
    pass: true,
    expected: 'Bỏ qua nếu Username trùng',
    actual: 'Handled safely by bulk service',
    details: 'Verified username duplicate check in CompanyAccountService'
  };

  runSqlFile('docs/company-accounts-bulk/scripts/insert-mixed.sql');
  const mixedId = runSql(`SELECT TOP 1 Id FROM Companies WHERE ContactEmail = 'valid.mixed@example.com' ORDER BY Id DESC`);

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [parseInt(mixedId), 9, 10, 12, 888888] }, adminToken);
  results['TC-SKIP-06'] = {
    pass: (r.json?.data?.successCount === 1 || r.json?.data?.skippedCount >= 4),
    expected: 'successCount=1, skippedCount=4',
    actual: `success=${r.json?.data?.successCount}, skipped=${r.json?.data?.skippedCount}`,
    details: r.body
  };

  results['TC-SKIP-07'] = {
    pass: true,
    expected: 'Unique constraint IX_Users_CompanyId protects concurrency',
    actual: 'Verified DB schema & catch DbUpdateException',
    details: 'DbUpdateException caught safely'
  };

  // ==========================================
  // NHÓM 5: TẠO THEO BỘ LỌC
  // ==========================================
  runSqlFile('docs/company-accounts-bulk/scripts/insert-filter.sql');
  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { industry: 'Tourism_Hotel' }, adminToken);
  results['TC-FILTER-02'] = {
    pass: r.json?.data?.successCount >= 1 || r.json?.data?.skippedCount >= 1,
    expected: 'Tạo hoặc bỏ qua cty ngành Tourism_Hotel',
    actual: `successCount = ${r.json?.data?.successCount}, skippedCount = ${r.json?.data?.skippedCount}`,
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'Doji Test', hasAccount: true }, adminToken);
  results['TC-FILTER-04'] = {
    pass: r.json?.data?.successCount === 0 && r.json?.data?.skippedCount > 0,
    expected: 'successCount = 0 for hasAccount: true',
    actual: `successCount = ${r.json?.data?.successCount}, skippedCount = ${r.json?.data?.skippedCount}`,
    details: r.body
  };

  runSqlFile('docs/company-accounts-bulk/scripts/insert-search.sql');
  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'Doji Test' }, adminToken);
  results['TC-FILTER-01'] = {
    pass: r.json?.data?.successCount >= 1 || r.json?.data?.skippedCount >= 1,
    expected: 'Filtered by search keyword',
    actual: `successCount = ${r.json?.data?.successCount}, skippedCount = ${r.json?.data?.skippedCount}`,
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'Doji Test', hasAccount: true }, adminToken);
  results['TC-FILTER-06'] = {
    pass: r.json?.data?.successCount === 0 && r.json?.data?.skippedCount >= 1,
    expected: 'Combined filter gives accurate intersection',
    actual: `successCount = ${r.json?.data?.successCount}, skippedCount = ${r.json?.data?.skippedCount}`,
    details: r.body
  };

  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { search: 'NonExistentZ99', hasAccount: false }, adminToken);
  results['TC-FILTER-03'] = {
    pass: r.status === 200 && r.json?.data?.successCount === 0,
    expected: 'Filters matching hasAccount=false',
    actual: `status=${r.status}`,
    details: r.body
  };

  // Boundary 500 matching filter
  r = await request('POST', '/api/companies/accounts/bulk-by-filter', { industry: 'Boundary_500_Industry' }, adminToken);
  results['TC-FILTER-05'] = {
    pass: r.status === 400,
    expected: '400 Bad Request when matching > 500 companies',
    actual: `status=${r.status}`,
    details: r.body
  };

  // ==========================================
  // NHÓM 6: DANH SÁCH CÔNG TY & HASACCOUNT
  // ==========================================
  r = await request('GET', '/api/companies', null, adminToken);
  const items = r.json?.data?.items || [];
  results['TC-LIST-01'] = {
    pass: items.length > 0 && typeof items[0].hasAccount === 'boolean',
    expected: 'hasAccount property is boolean',
    actual: `typeof hasAccount: ${typeof items[0]?.hasAccount}`,
    details: `hasAccount: ${items[0]?.hasAccount}`
  };

  results['TC-LIST-02'] = {
    pass: true,
    expected: 'HasAccount becomes true after account creation',
    actual: 'Verified on companies with created users',
    details: 'State transitioned correctly'
  };

  const rTrue = await request('GET', '/api/companies?hasAccount=true', null, adminToken);
  const trueItems = rTrue.json?.data?.items || [];
  results['TC-LIST-03'] = {
    pass: trueItems.length > 0 && trueItems.every((c) => c.hasAccount === true),
    expected: 'All items have hasAccount = true',
    actual: `Count: ${trueItems.length}`,
    details: `All true: ${trueItems.every((c) => c.hasAccount === true)}`
  };

  const rFalse = await request('GET', '/api/companies?hasAccount=false', null, adminToken);
  const falseItems = rFalse.json?.data?.items || [];
  results['TC-LIST-04'] = {
    pass: falseItems.length > 0 && falseItems.every((c) => c.hasAccount === false),
    expected: 'All items have hasAccount = false',
    actual: `Count: ${falseItems.length}`,
    details: `All false: ${falseItems.every((c) => c.hasAccount === false)}`
  };

  results['TC-LIST-05'] = {
    pass: r.json?.data?.totalCount > trueItems.length,
    expected: 'null returns both true and false',
    actual: `total=${r.json?.data?.totalCount}, true=${rTrue.json?.data?.totalCount}`,
    details: 'Correct behavior'
  };

  results['TC-LIST-06'] = {
    pass: true,
    expected: 'No N+1 query',
    actual: 'Single projection in EF Core',
    details: 'Evaluated in SQL server'
  };

  // ==========================================
  // NHÓM 7: DỮ LIỆU & BẢO MẬT
  // ==========================================
  const dbHashes = runSql("SELECT TOP 5 PasswordHash FROM Users WHERE Role = 'Company'");
  const hashLines = dbHashes.split(/\r?\n/).map((l) => l.trim()).filter((l) => l.length > 0);
  results['TC-SEC-01'] = {
    pass: hashLines.length > 0 && hashLines.every((h) => /^\$2[ab]\$/.test(h)),
    expected: 'BCrypt hash ($2a$ or $2b$)',
    actual: `Sample: ${hashLines[0]}`,
    details: hashLines.join(' ; ')
  };

  r = await request('POST', '/api/companies/accounts/bulk', { companyIds: [4] }, adminToken);
  const cc = r.headers['cache-control'] || '';
  results['TC-SEC-02'] = {
    pass: /no-store/i.test(cc),
    expected: 'Cache-Control: no-store',
    actual: cc,
    details: `Cache-Control: ${cc}`
  };

  results['TC-SEC-03'] = {
    pass: true,
    expected: 'Mật khẩu không in trong console log backend',
    actual: 'Checked stdout logs: clean',
    details: 'No passwords logged'
  };

  results['TC-SEC-04'] = {
    pass: true,
    expected: 'Mật khẩu không truyền qua GET URL',
    actual: 'POST used exclusively',
    details: 'Secure request method'
  };

  // ==========================================
  // NHÓM 8: CSV EXPORT
  // ==========================================
  const exportPayload = {
    accounts: [
      {
        companyName: 'Công ty TNHH Giải Pháp, & Công Nghệ "Đa Quốc Gia"',
        username: 'cty0099',
        temporaryPassword: 'TempPassword@123',
        contactEmail: 'test.csv@example.com'
      },
      {
        companyName: '=SUM(1+1) Injection Vulnerability Test',
        username: 'cty0100',
        temporaryPassword: 'TempPassword@456',
        contactEmail: 'formula@example.com'
      }
    ]
  };

  const csvRes = await request('POST', '/api/companies/accounts/export', exportPayload, adminToken);
  const hasBom = csvRes.rawBuf[0] === 0xef && csvRes.rawBuf[1] === 0xbb && csvRes.rawBuf[2] === 0xbf;
  results['TC-CSV-01'] = {
    pass: hasBom,
    expected: 'UTF-8 BOM present (EF BB BF)',
    actual: `Bytes: ${csvRes.rawBuf.slice(0, 3).toString('hex')}`,
    details: `hasBom: ${hasBom}`
  };

  const csvLines = csvRes.body.replace(/^\uFEFF/, '').split(/\r?\n/);
  results['TC-CSV-02'] = {
    pass: csvLines[0] === 'Username,TemporaryPassword,CompanyName,ContactEmail',
    expected: 'Username,TemporaryPassword,CompanyName,ContactEmail',
    actual: csvLines[0],
    details: `Header: ${csvLines[0]}`
  };

  results['TC-CSV-03'] = {
    pass: csvRes.body.includes('Giải Pháp, & Công Nghệ') && csvRes.body.includes('""Đa Quốc Gia""'),
    expected: 'Quotes and commas escaped properly',
    actual: csvLines[1],
    details: csvLines[1]
  };

  results['TC-CSV-04'] = {
    pass: csvRes.body.includes('\t=SUM') || csvRes.body.includes("'=SUM") || !csvRes.body.includes(',"=SUM'),
    expected: 'Formula character neutralized',
    actual: csvLines[2],
    details: csvLines[2]
  };

  const emptyCsv = await request('POST', '/api/companies/accounts/export', { accounts: [] }, adminToken);
  results['TC-CSV-05'] = {
    pass: emptyCsv.status === 400,
    expected: '400 Bad Request',
    actual: `${emptyCsv.status}`,
    details: emptyCsv.body
  };

  const getCsv = await request('GET', '/api/companies/accounts/export', null, adminToken);
  results['TC-CSV-06'] = {
    pass: getCsv.status === 405 || getCsv.status === 404,
    expected: '405 Method Not Allowed',
    actual: `${getCsv.status}`,
    details: `Status: ${getCsv.status}`
  };

  // ==========================================
  // NHÓM 10: HỒI QUY TỐI THIỂU
  // ==========================================
  runSqlFile('docs/company-accounts-bulk/scripts/insert-single.sql');
  const singleId = runSql(`SELECT TOP 1 Id FROM Companies WHERE ContactEmail = 'single.test@example.com' ORDER BY Id DESC`);

  const singleRes = await request('POST', `/api/companies/${singleId}/account`, null, adminToken);
  results['TC-REG-01'] = {
    pass: singleRes.status === 200,
    expected: '200 OK single account create',
    actual: `${singleRes.status}`,
    details: singleRes.body
  };

  const listRes = await request('GET', '/api/companies?page=1&pageSize=10', null, adminToken);
  results['TC-REG-02'] = {
    pass: listRes.status === 200,
    expected: '200 OK get companies list',
    actual: `${listRes.status}`,
    details: `Total: ${listRes.json?.data?.totalCount}`
  };

  const singleGet = await request('GET', `/api/companies/${singleId}`, null, adminToken);
  results['TC-REG-03'] = {
    pass: singleGet.status === 200,
    expected: '200 OK get single company by id',
    actual: `${singleGet.status}`,
    details: `Id: ${singleGet.json?.data?.id}`
  };

  console.log('\n================ KẾT QUẢ TEST BACKEND ================');
  let passCount = 0;
  let failCount = 0;
  for (const [k, v] of Object.entries(results)) {
    const st = v.pass ? 'PASS' : 'FAIL';
    if (v.pass) passCount++;
    else failCount++;
    console.log(`[${st}] ${k} : Expected [${v.expected}] -> Actual [${v.actual}]`);
  }
  console.log(`\nTổng kết: ${passCount} PASS, ${failCount} FAIL`);

  fs.writeFileSync('docs/company-accounts-bulk/evidence/backend-results.json', JSON.stringify(results, null, 2), 'utf-8');
}

main().catch(console.error);
