// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Http.Headers;

Console.WriteLine("Hello, World!");


//var cookieContainer = new CookieContainer();
//var handler = new HttpClientHandler
//{
//    CookieContainer = cookieContainer,
//    UseCookies = true
//};

var cookie = @"_ym_uid=172464149064425483; uuid=e55c363e-37b8-46db-a3ea-01fa2949fee1; visitorID=38477324; psid2=6h4w9dul06mtar81qmsbjx71j8c97avw; spid=1739156007643_eb83e6f78c58689510f72755e55a053e_c8vdw9adi4ih6slj; canary1={""tags_predicto"":{""active"":""A"",""term"":[{""id"":44,""label"":""A"",""percent"":100},{""id"":45,""label"":""B"",""percent"":100}]},""new_player"":{""active"":""A"",""term"":[{""id"":50,""label"":""A"",""percent"":100},{""id"":51,""label"":""B"",""percent"":100}]},""banner_videopage"":{""active"":""A"",""term"":[{""id"":52,""label"":""A"",""percent"":100},{""id"":53,""label"":""B"",""percent"":100}]},""banner_naz_mainpage"":{""active"":""A"",""term"":[{""id"":56,""label"":""A"",""percent"":100},{""id"":57,""label"":""B"",""percent"":100}]},""banner_az_mainpage"":{""active"":""A"",""term"":[{""id"":54,""label"":""A"",""percent"":100},{""id"":55,""label"":""B"",""percent"":100}]}}; _ym_d=1757913466; uxs_uid=5d0104a0-9db3-11f0-ac45-4b5e2701dda1; refreshToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJydXBhc3NfaWQiOjMzNDg1MjA1LCJleHAiOjE3Njk4MzQ3MTgsInJlcXVlc3RfaWQiOiI3MDI4YzY0NC1lMGE2LTRiNmEtYTgzMi1lMzc4NzAzODAyYmMifQ.QYOaZez22yF_MUYb6DSM6lhWQM6yjwt6IODXYPn6KDw; jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJydXBhc3NfaWQiOjMzNDg1MjA1LCJlbWFpbF9jb25maXJtZWQiOnsidHlwZW9mIjoiYm9vbCIsInZhbHVlIjp0cnVlfSwiaGFzX3Bob25lIjp7InR5cGVvZiI6ImJvb2wiLCJ2YWx1ZSI6dHJ1ZX0sImV4cCI6MTc2NzI0MjcxOCwib3JpZ19pYXQiOjE3NjQ2NTA3MTgsInVzZXJfaWQiOjM4NDc3MzI0LCJkYXRhIjp7InVzZXJfaW5mbyI6eyJpZCI6Mzg0NzczMjR9fSwidXNlcm5hbWUiOiJib2IyMTdAbWFpbC5ydSIsImVtYWlsIjoiYm9iMjE3QG1haWwucnUiLCJuYW1lIjoiXHUwNDFiXHUwNDQzXHUwNDQ3XHUwNDM4XHUwNDNhIFx1MDQ0MVx1MDQzMlx1MDQzNVx1MDQ0Mlx1MDQzMCBcdTA0M2ZcdTA0NDBcdTA0M2VcdTA0MzRcdTA0NDNcdTA0M2FcdTA0NDJcdTA0M2VcdTA0MzJcdTA0M2VcdTA0MzkgXHUwNDQwXHUwNDMwXHUwNDM3XHUwNDQwXHUwNDMwXHUwNDMxXHUwNDNlXHUwNDQyXHUwNDNhXHUwNDM4In0.Vf3ZSx54IeFDBxT2j_G0fMCVMiCbreZU_dzoVtdfnMo; _ym_isad=1; session_id=94374422931724641490_1765983770547; csrftoken=6f4d3f0181494c66b9802dfc037a7c2d; _ym_visorc=b; qrator_msid2=v2.0.1765983768.819.5abc5e8c0meiotMf|BHQFiE92SftuvNXd|9husM0iFh6ggM1AO7Y0aU+zeWpWsqsmL7HDAlxzPDtR/LakEO1EXPeFXiuVnNxqfx0LqG+fKWgoEXwJpd5BuMw==-JlwI2/M6qd2192XsFs69mfFxgYY=; cid=94374422931724641490";
//cookieContainer.Add(new Uri("https://studio.rutube.ru"),
//    new Cookie("sessionId", "abc123")
//    {
//        Domain = "example.com",
//        Path = "/"
//    });

using var client = new HttpClient();// handler);
using var content = new MultipartFormDataContent();

// Если есть файл на диске
var fileBytes = await File.ReadAllBytesAsync("C:\\Users\\Max\\Videos\\124.mp4");
var fileContent = new ByteArrayContent(fileBytes);
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/mp4");
content.Add(fileContent, "file", "document.pdf");


// Дополнительные параметры
content.Add(new StringContent("123"), "userId");
client.DefaultRequestHeaders.Add("Cookie", cookie);
client.Timeout = new TimeSpan(0,0,5);
var response = await client.PostAsync("https://studio.rutube.ru/api/uploader/upload_session/?client=vulp&batch_id=f0dd6feac80000c19bfa756c931f07ad", content);

Console.WriteLine(response.StatusCode);
Console.WriteLine(response.Content.ToString());

