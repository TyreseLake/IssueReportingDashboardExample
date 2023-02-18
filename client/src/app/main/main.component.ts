import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { IssueReportingService } from '../_services/issue-reporting.service';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class MainComponent implements OnInit {
  info : any;
  issueTypeTotals: any;
  districtTotals: any;
  pinnedIssues = [];
  pinnedIssuesCount;
  recentUpdates = [];
  recentUpdatesCount;
  recentAdditions = []
  recentAdditionsCount;

  constructor(private issueReportingService: IssueReportingService, public accountService: AccountService) { }

  ngOnInit(): void {
    this.issueReportingService.getIssuesSummary().subscribe({
      next: (result) => {
        this.info = result;
        this.loadIssueTypeTotalsChart();
        this.loadDistrictTotalsChart();
        this.loadPinnedIssueReports();
        this.loadRecentUpdatedReports();
        this.loadRecentAddedReports();
      }
    });
  }

  loadIssueTypeTotalsChart() {
    if(this.info['issueTypeTotals'] != null){
      this.issueTypeTotals = {}
      var issueTypeTotalsValues = this.info['issueTypeTotals'].map(({issueType, count}) => ([issueType, count]))
      this.issueTypeTotals['title'] = 'Issue Type Totals'
      this.issueTypeTotals['type'] = 'PieChart'
      this.issueTypeTotals['data'] = issueTypeTotalsValues;
      this.issueTypeTotals['columnNames'] = ['Issue Type', 'No. of Issues'];
      this.issueTypeTotals['options'] = {
        chartArea:{left:10,top:26, right:10, bottom:10},
        fontName:'Segoe UI',
        pieSliceText: 'value-and-percentage',
        tooltip:{ignoreBounds: true}
      };
      this.issueTypeTotals['width'] = 428;
      this.issueTypeTotals['height'] = 250;
    }
  }

  loadDistrictTotalsChart() {
    if(this.info['districtTotals'] != null){
      this.districtTotals = {}
      var districtTotalsValues = this.info['districtTotals'].map(({district, count}) => ([district, count]))
      this.districtTotals['title'] = 'District Totals'
      this.districtTotals['type'] = 'PieChart'
      this.districtTotals['data'] = districtTotalsValues;
      this.districtTotals['columnNames'] = ['District', 'No. of Issues'];
      this.districtTotals['options'] = {
        chartArea:{left:10,top:26, right:10, bottom:10},
        fontName:'Segoe UI',
        pieSliceText: 'value-and-percentage',
        tooltip:{ignoreBounds: true}
      };
      this.districtTotals['width'] = 428;
      this.districtTotals['height'] = 250;
    }
  }

  loadRecentUpdatedReports(){
    if(this.info?.['recentUpdates']) {
      this.recentUpdates = this.info['recentUpdates'];
      this.recentUpdatesCount = this.info['recentUpdatesCount']
    }
  }

  loadRecentAddedReports(){
    if(this.info?.['recentAdditions']) {
      this.recentAdditions = this.info['recentAdditions'];
      this.recentAdditionsCount = this.info['recentAdditionsCount'];
    }
  }

  loadPinnedIssueReports(){
    if(this.info?.['pinnedIssues']) {
      this.pinnedIssues = this.info['pinnedIssues'];
      this.pinnedIssuesCount = this.info['pinnedIssuesCount'];
    }
  }
}
