param ([string]$path)

$meta_version = "0.3.3"
$meta_description = "RedBeanPHP-inspired data access layer"
$meta_author = "Aleksey Martynov"
$meta_copyright = "Copyright (c) 2014-$(get-date -format yyyy) $meta_author"
$meta_project_url = "https://github.com/AlekseyMartynov/LimeBean"
$meta_license_url = "https://raw.githubusercontent.com/AlekseyMartynov/LimeBean/master/LICENSE.txt"

(Get-Content $path) | Foreach-Object {
    $_  -replace '(AssemblyVersion.+?)[\d.]+', "`${1}%lime_version%" `
        -replace '("version":.+?)[\d.]+', "`${1}%lime_version%" `
        -replace '(\<version\>)[\d.]+', "`${1}%lime_version%" `
        -replace '%lime_version%', $meta_version `
        -replace '%lime_description%', $meta_description `
        -replace '%lime_author%', $meta_author `
        -replace '%lime_copyright%', $meta_copyright `
        -replace '%lime_project_url%', $meta_project_url `
        -replace '%lime_license_url%', $meta_license_url
} | Set-Content $path
