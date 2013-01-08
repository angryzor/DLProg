if page.UserData == null then
  for url in string.gmatch(page.HTML,"<li><a href='(http://coda.s3m.us/20[0-9][0-9]/[0-9][0-9]/)' title=") do
    queue:add_page_link(url,1)
  end
else
  for url in string.gmatch(page.HTML,'<a href="([^"]+)">download</a>') do
    queue:add_download_link(url,"",string.match(url,'/([^/]+)$'))
  end
end
