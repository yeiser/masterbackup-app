import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BackupExecutionListComponent } from './backup-execution-list.component';

describe('BackupExecutionListComponent', () => {
  let component: BackupExecutionListComponent;
  let fixture: ComponentFixture<BackupExecutionListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BackupExecutionListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BackupExecutionListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
